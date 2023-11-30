/*using Compiler1.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Compiler1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Text()
        {
            var model = new TextModel(); // Tạo một instance của TextModel
            return View(model); // Truyền model vào view
        }

        [HttpPost]
        public IActionResult Run(TextModel model)
        {
            if (string.IsNullOrEmpty(model.InputText))
            {
                // Xử lý trường hợp không có dữ liệu đầu vào (ví dụ: hiển thị thông báo lỗi)
                ViewBag.Result = "Please enter some text before running.";
            }
            else
            {
                // Xử lý đoạn văn bản trong model.InputText và lưu kết quả vào ViewBag.Result
                ViewBag.Result = ProcessText(model.InputText);
            }

            return View("Index", model);
        }

        private string ProcessText(string inputText)
        {
            // Thực hiện xử lý đoạn văn bản ở đây
            char[] charArray = inputText.ToCharArray();
            string inText = new string(charArray);

            return inText; // Trả về kết quả xử lý
        }
    }
}
*/

/*using Compiler1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> RunCompiler([FromBody] TextModel model)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", "d7f29a1eaemsh4463d4ba4e6aa3fp110fd8jsn9279a32bc497");
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "online-code-compiler.p.rapidapi.com");

            var requestBody = new
            {
                language = "python3",
                version = "latest",
                code = model.InputText,
                input = model.Input
            };

            var response = await client.PostAsJsonAsync("https://online-code-compiler.p.rapidapi.com/v1/", requestBody);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            ViewBag.Result = result;

            return View("Index", model);
        }
    }
}*/

using Compiler1.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;

[ApiController]
[Route("api/compiler")]
public class HomeController : ControllerBase
{
    [HttpPost("compile")]
    public async Task<IActionResult> Compile([FromBody] CodeRequest request)
    {
        try
        {
            // Xử lý mã nguồn và trả về kết quả
            var compiledOutput = await CompileCodeAsync(request.Code, request.Input, request.Language);

            // Gửi kết quả về cho client
            var response = new CodeResponse { Output = compiledOutput };
            return Ok(response);
        }
        catch (Exception ex)
        {
            // Xử lý lỗi và trả về phản hồi lỗi
            return StatusCode(500, new { error = "Internal Server Error", message = ex.Message });
        }
    }

    private async Task<string> CompileCodeAsync(string code, string input, string language)
    {
        string compilerPath = "C:\\msys64\\mingw64\\bin\\g++.exe"; // Đường dẫn đến trình biên dịch C++ (cần cài đặt g++)

        // Lưu mã nguồn vào một tệp tạm thời
        string sourceFilePath = Path.Combine(Path.GetTempPath(), "temp.cpp");
        await System.IO.File.WriteAllTextAsync(sourceFilePath, code);

        // Xác định đường dẫn đến tệp thực thi (nếu biên dịch thành công)
        string executablePath = Path.Combine(Path.GetTempPath(), "temp.exe");

        try
        {
            // Tạo quy trình để gọi trình biên dịch
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = compilerPath,
                Arguments = $"-o \"{executablePath}\" \"{sourceFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                // Thực hiện biên dịch
                process.Start();
                process.WaitForExit();

                // Kiểm tra xem có lỗi nào xuất hiện không
                if (process.ExitCode != 0)
                {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    return $"Compilation Error: {errorOutput}";
                }

                // Nếu không có lỗi, thực hiện thực thi chương trình đã biên dịch với dữ liệu nhập
                psi.FileName = executablePath;
                psi.Arguments = "";

                using (Process executionProcess = new Process { StartInfo = psi })
                {
                    executionProcess.StartInfo.RedirectStandardInput = true;
                    executionProcess.StartInfo.RedirectStandardOutput = true;

                    // Thực hiện thực thi chương trình với dữ liệu nhập
                    executionProcess.Start();

                    if (!string.IsNullOrEmpty(input))
                    {
                        await executionProcess.StandardInput.WriteAsync(input);
                        executionProcess.StandardInput.Close();
                    }

                    string output = await executionProcess.StandardOutput.ReadToEndAsync();

                    // Đóng tất cả các tiến trình
                    executionProcess.WaitForExit();
                    return output;
                }
            }
        }
        finally
        {
            // Xóa các tệp tạm thời
            System.IO.File.Delete(sourceFilePath);
            System.IO.File.Delete(executablePath);
        }
    }

}



