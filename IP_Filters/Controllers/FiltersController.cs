using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace IP_Filters.Controllers
{
    public class FiltersController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public FiltersController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GrayColor(IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                using var stream = imageFile.OpenReadStream();
                using var original = new Bitmap(stream);
                using var grayImage = new Bitmap(original.Width, original.Height);

                var imagesPath = Path.Combine(_environment.WebRootPath, "images");
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                var originalFileName = $"original_{Path.GetRandomFileName()}.png";
                var originalFullPath = Path.Combine(imagesPath, originalFileName);
                original.Save(originalFullPath, ImageFormat.Png);
                TempData["OriginalImage"] = "/images/" + originalFileName;

                for (int y = 0; y < original.Height; y++)
                {
                    for (int x = 0; x < original.Width; x++)
                    {
                        var pixel = original.GetPixel(x, y);
                        int gray = (int)(0.3 * pixel.R + 0.59 * pixel.G + 0.11 * pixel.B);
                        var grayColor = Color.FromArgb(gray, gray, gray);
                        grayImage.SetPixel(x, y, grayColor);
                    }
                }

                var grayFileName = $"gray_{Path.GetRandomFileName()}.png";
                var grayFullPath = Path.Combine(imagesPath, grayFileName);
                grayImage.Save(grayFullPath, ImageFormat.Png);
                TempData["ResultImage"] = "/images/" + grayFileName;

                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Please upload an image.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult DefaultColor(IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                using var stream = imageFile.OpenReadStream();
                using var image = new Bitmap(stream);

                var imagesPath = Path.Combine(_environment.WebRootPath, "images");
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                var fileName = $"original_{Path.GetRandomFileName()}.png";
                var fullPath = Path.Combine(imagesPath, fileName);
                image.Save(fullPath, ImageFormat.Png);

                TempData["OriginalImage"] = "/images/" + fileName;
                TempData["ResultImage"] = "/images/" + fileName;

                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Please upload an image.";
            return RedirectToAction("Index", "Home");
        }
    }
}
