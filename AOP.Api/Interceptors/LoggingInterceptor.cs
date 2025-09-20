using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace AOP.Api.Interceptors
{
    public class LoggingInterceptor : IInterceptor
    {
        private readonly ILogger<LoggingInterceptor> _logger;
        private static readonly HashSet<string> _processedFlows = new();

        public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? "no-trace";
            var spanId = Activity.Current?.SpanId.ToString();
            var parentSpanId = Activity.Current?.ParentSpanId.ToString();

            var methodContext = new
            {
                Layer = GetApplicationLayer(invocation.TargetType),
                Class = invocation.TargetType.Name,
                Method = invocation.Method.Name,
                FullPath = $"{invocation.Method.DeclaringType?.FullName}.{invocation.Method.Name}",
                Namespace = invocation.Method.DeclaringType?.Namespace ?? "Unknown"
            };

            var flowKey = $"{methodContext.Layer}.{methodContext.Class}.{methodContext.Method}";
            var isNewFlow = _processedFlows.Add(flowKey);

            var arguments = SerializeArguments(invocation.Arguments, invocation.Method.GetParameters());
            var caller = GetCallerInfo();

            // Log Begin Transaction with documentation metadata
            _logger.LogInformation("{@FlowEvent}", new
            {
                Event = "Begin Transaction",
                Flow = new
                {
                    Key = flowKey,
                    IsNewFlow = isNewFlow,
                    Type = "Method Call",
                    Direction = "In",
                    Caller = caller,
                    ApiEndpoint = GetApiEndpointInfo(invocation)
                },
                Context = methodContext,
                Arguments = arguments,
                Correlation = new
                {
                    TraceId = traceId,
                    SpanId = spanId,
                    ParentSpanId = parentSpanId
                },
                Timestamp = DateTimeOffset.UtcNow
            });

            var stopwatch = Stopwatch.StartNew();
            try
            {
                invocation.Proceed();
                stopwatch.Stop();

                // Log End Transaction with documentation metadata
                _logger.LogInformation("{@FlowEvent}", new
                {
                    Event = "End Transaction",
                    Flow = new
                    {
                        Key = flowKey,
                        Type = "Method Return",
                        Direction = "Out",
                        ResponseType = invocation.Method.ReturnType.Name,
                        Duration = new
                        {
                            Milliseconds = stopwatch.ElapsedMilliseconds,
                            Ticks = stopwatch.ElapsedTicks
                        }
                    },
                    Context = methodContext,
                    Result = SerializeValue(invocation.ReturnValue),
                    Correlation = new
                    {
                        TraceId = traceId,
                        SpanId = spanId
                    },
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Log Transaction Failed with documentation metadata
                _logger.LogError(ex, "{@FlowEvent}", new
                {
                    Event = "Transaction Failed",
                    Flow = new
                    {
                        Key = flowKey,
                        Type = "Error",
                        Direction = "Error",
                        Duration = new
                        {
                            Milliseconds = stopwatch.ElapsedMilliseconds,
                            Ticks = stopwatch.ElapsedTicks
                        }
                    },
                    Context = methodContext,
                    Error = new
                    {
                        Message = ex.Message,
                        Type = ex.GetType().Name,
                        StackTrace = ex.StackTrace
                    },
                    Correlation = new
                    {
                        TraceId = traceId,
                        SpanId = spanId
                    },
                    Timestamp = DateTimeOffset.UtcNow
                });
                throw;
            }
        }

        private string GetApplicationLayer(Type targetType)
        {
            var fullName = targetType.FullName ?? string.Empty;
            if (fullName.Contains(".Controllers.")) return "Controller";
            if (fullName.Contains(".Services.")) return "Service";
            if (fullName.Contains(".Repositories.")) return "Repository";
            return "Unknown";
        }

        private object SerializeArguments(object[] arguments, System.Reflection.ParameterInfo[] parameters)
        {
            try
            {
                var namedArgs = new Dictionary<string, object?>();
                for (int i = 0; i < parameters.Length; i++)
                {
                    var value = i < arguments.Length ? arguments[i] : null;
                    namedArgs[parameters[i].Name ?? $"arg{i}"] = value;
                }
                return namedArgs;
            }
            catch
            {
                return new { error = "Serialization Failed" };
            }
        }

        private object? SerializeValue(object? value)
        {
            try
            {
                return value;
            }
            catch
            {
                return new { error = "Serialization Failed" };
            }
        }

        private object? GetCallerInfo()
        {
            try
            {
                var stack = new StackTrace(true);
                var frames = stack.GetFrames();
                var relevantFrame = frames?.FirstOrDefault(f =>
                    f.GetMethod()?.DeclaringType?.Namespace?.Contains(".Controllers") == true ||
                    f.GetMethod()?.DeclaringType?.Namespace?.Contains(".Services") == true);

                if (relevantFrame != null)
                {
                    return new
                    {
                        Method = relevantFrame.GetMethod()?.Name,
                        Class = relevantFrame.GetMethod()?.DeclaringType?.Name,
                        File = relevantFrame.GetFileName(),
                        Line = relevantFrame.GetFileLineNumber()
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private object? GetApiEndpointInfo(IInvocation invocation)
        {
            if (!invocation.TargetType.Name.EndsWith("Controller"))
                return null;

            try
            {
                var methodInfo = invocation.Method;
                var httpAttributes = methodInfo.GetCustomAttributes(true)
                    .Where(attr => attr.GetType().Name.StartsWith("Http"))
                    .Select(attr => attr.GetType().Name.Replace("Http", "").Replace("Attribute", ""))
                    .FirstOrDefault();

                return new
                {
                    HttpMethod = httpAttributes,
                    Route = methodInfo.Name.Replace("Async", "")
                };
            }
            catch
            {
                return null;
            }
        }
    }
}