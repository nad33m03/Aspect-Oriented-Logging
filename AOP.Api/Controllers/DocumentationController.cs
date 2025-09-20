using Microsoft.AspNetCore.Mvc;
using AOP.Api.Documentation;

namespace AOP.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentationController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    public DocumentationController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var logPath = Path.Combine(_environment.ContentRootPath, "logs", "app.log");
        var outputPath = Path.Combine(_environment.ContentRootPath, "docs", "flow.md");
        
        // Ensure docs directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        // Generate documentation
        await FlowDocumentationGenerator.GenerateMarkdownDoc(logPath, outputPath);
        
        // Read and return as HTML
        var markdown = await System.IO.File.ReadAllTextAsync(outputPath);
        return Content($@"
            <html>
            <head>
                <script src='https://cdn.jsdelivr.net/npm/marked/marked.min.js'></script>
            </head>
            <body>
                <div id='content'></div>
                <script>
                    document.getElementById('content').innerHTML = marked.parse(`{markdown.Replace("`", "\\`")}`);
                </script>
            </body>
            </html>", 
            "text/html");
    }
}