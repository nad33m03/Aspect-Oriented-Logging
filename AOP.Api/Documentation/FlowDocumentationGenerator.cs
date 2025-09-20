using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AOP.Api.Documentation
{
    public class FlowDocumentationGenerator
    {
        public static async Task GenerateMarkdownDoc(string logFilePath, string outputPath)
        {
            List<string> logs = new();
            // Open the file with shared read/write access
            using (var stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    logs.Add(line);
                }
            }

            var flowGroups = logs
                .Where(log => log.Contains("[INF]") && log.Contains("\"Event\""))
                .Select(log => ExtractJsonFromLog(log))
                .Where(json => !string.IsNullOrEmpty(json))
                .Select(json => 
                {
                    try 
                    {
                        return JsonSerializer.Deserialize<FlowEvent>(json);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(e => e != null)
                .GroupBy(e => e?.Correlation?.TraceId);

            var markdown = new StringBuilder();
            markdown.AppendLine("# Application Flow Documentation\n");

            foreach (var flow in flowGroups)
            {
                if (flow.Key == null) continue;

                var events = flow.OrderBy(e => e?.Timestamp).ToList();
                var firstEvent = events.FirstOrDefault();
                
                // Get API endpoint info from the first event
                var apiEndpoint = GetApiEndpointInfo(events);
                if (apiEndpoint != null)
                {
                    markdown.AppendLine($"## API Endpoint: {apiEndpoint.HttpMethod} {apiEndpoint.Route}\n");
                    markdown.AppendLine("### Flow Sequence\n");
                    
                    foreach (var evt in events)
                    {
                        var direction = evt?.Flow?.Direction == "In" ? "→" : "←";
                        markdown.AppendLine($"- {direction} **{evt?.Context?.Layer}**: `{evt?.Context?.Class}.{evt?.Context?.Method}`");
                        
                        // Add caller information if available
                        if (evt?.Flow?.Caller != null)
                        {
                            markdown.AppendLine($"  - Called by: `{evt.Flow.Caller.Class}.{evt.Flow.Caller.Method}` (Line: {evt.Flow.Caller.Line})");
                        }
                        
                        if (evt?.Arguments != null)
                        {
                            markdown.AppendLine($"  - Parameters: ```json\n{JsonSerializer.Serialize(evt.Arguments, new JsonSerializerOptions { WriteIndented = true })}```");
                        }
                        
                        if (evt?.Flow?.Duration != null)
                        {
                            markdown.AppendLine($"  - Duration: {evt.Flow.Duration.Milliseconds}ms");
                        }

                        // Add response type for "Out" direction
                        if (evt?.Flow?.Direction == "Out" && evt?.Flow?.ResponseType != null)
                        {
                            markdown.AppendLine($"  - Returns: `{evt.Flow.ResponseType}`");
                        }
                    }
                    
                    // Add total flow duration
                    var totalDuration = events
                        .Where(e => e?.Flow?.Duration != null)
                        .Sum(e => e?.Flow?.Duration?.Milliseconds ?? 0);
                    markdown.AppendLine($"\n**Total Flow Duration:** {totalDuration}ms");
                    
                    markdown.AppendLine("\n---\n");
                }
            }

            await File.WriteAllTextAsync(outputPath, markdown.ToString());
        }

        private static string? ExtractJsonFromLog(string logLine)
        {
            var match = Regex.Match(logLine, @"\[INF\]\s+(\{.*\})");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static ApiEndpointInfo? GetApiEndpointInfo(List<FlowEvent?> events)
        {
            // Try to get API endpoint from Flow.ApiEndpoint first
            var endpointFromFlow = events
                .FirstOrDefault(e => e?.Flow?.ApiEndpoint != null)
                ?.Flow?.ApiEndpoint;

            if (endpointFromFlow != null)
                return endpointFromFlow;

            // Try to find a controller event
            var controllerEvent = events
                .FirstOrDefault(e => e?.Context?.Layer == "Controller");

            if (controllerEvent != null)
            {
                return new ApiEndpointInfo
                {
                    HttpMethod = ExtractHttpMethodFromName(controllerEvent.Context?.Method ?? ""),
                    Route = FormatRouteFromController(controllerEvent.Context?.Class ?? "", controllerEvent.Context?.Method ?? "")
                };
            }

            // Fallback: Try to infer from Service event's Caller
            var serviceEvent = events
                .FirstOrDefault(e => e?.Context?.Layer == "Service" && e?.Flow?.Caller != null);

            if (serviceEvent?.Flow?.Caller != null)
            {
                var caller = serviceEvent.Flow.Caller;
                return new ApiEndpointInfo
                {
                    HttpMethod = ExtractHttpMethodFromName(caller.Method ?? ""),
                    Route = FormatRouteFromController(caller.Class ?? "", caller.Method ?? "")
                };
            }

            return null;
        }

        private static string ExtractHttpMethodFromName(string methodName)
        {
            if (methodName.StartsWith("Get")) return "GET";
            if (methodName.StartsWith("Post")) return "POST";
            if (methodName.StartsWith("Put")) return "PUT";
            if (methodName.StartsWith("Delete")) return "DELETE";
            if (methodName.StartsWith("Patch")) return "PATCH";
            return "Unknown";
        }

        private static string FormatRouteFromController(string controllerClass, string methodName)
        {
            var controller = controllerClass.Replace("Controller", "");
            return $"/{controller}/{methodName.Replace("Async", "")}";
        }

        private class FlowEvent
        {
            public string? Event { get; set; }
            public FlowInfo? Flow { get; set; }
            public ContextInfo? Context { get; set; }
            public object? Arguments { get; set; }
            public CorrelationInfo? Correlation { get; set; }
            public DateTime? Timestamp { get; set; }
        }

        private class FlowInfo
        {
            public string? Key { get; set; }
            public string? Type { get; set; }
            public string? Direction { get; set; }
            public ApiEndpointInfo? ApiEndpoint { get; set; }
            public DurationInfo? Duration { get; set; }
            public string? ResponseType { get; set; }
            public CallerInfo? Caller { get; set; }
        }

        private class ContextInfo
        {
            public string? Layer { get; set; }
            public string? Class { get; set; }
            public string? Method { get; set; }
        }

        private class CorrelationInfo
        {
            public string? TraceId { get; set; }
            public string? SpanId { get; set; }
        }

        private class ApiEndpointInfo
        {
            public string? HttpMethod { get; set; }
            public string? Route { get; set; }
        }

        private class DurationInfo
        {
            public long Milliseconds { get; set; }
            public long Ticks { get; set; }
        }

        private class CallerInfo
        {
            public string? Method { get; set; }
            public string? Class { get; set; }
            public string? File { get; set; }
            public int Line { get; set; }
        }
    }
}