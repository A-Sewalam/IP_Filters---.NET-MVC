using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System; // Required for Guid

namespace IP_Filters.Controllers
{
    public class FiltersController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private static readonly Random _random = new Random();
        private double _spareGaussian = double.NaN; // For Box-Muller helper

        public FiltersController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        // Helper to get a safe file name
        private string GetSafeFilename(string filename)
        {
            // Basic sanitization: replace invalid path chars, take last part if full path given
            var nameOnly = Path.GetFileName(filename);
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                nameOnly = nameOnly.Replace(c, '_');
            }
            return nameOnly;
        }

        [HttpPost]
        public IActionResult GrayColor(IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var imagesPath = Path.Combine(_environment.WebRootPath, "images");
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                var originalFileName = $"original_{Guid.NewGuid()}.png";
                var originalFullPath = Path.Combine(imagesPath, originalFileName);
                string originalWebPath = "/images/" + originalFileName;

                using (var stream = imageFile.OpenReadStream())
                using (var original = new Bitmap(stream))
                {
                    original.Save(originalFullPath, ImageFormat.Png);
                    HttpContext.Session.SetString("OriginalImagePath", originalWebPath);

                    using var grayImage = new Bitmap(original.Width, original.Height);
                    for (int y = 0; y < original.Height; y++)
                    {
                        for (int x = 0; x < original.Width; x++)
                        {
                            var pixel = original.GetPixel(x, y);
                            int grayVal = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B); // Standard luminance calculation
                            var grayColorValue = Color.FromArgb(pixel.A, grayVal, grayVal, grayVal); // Preserve alpha
                            grayImage.SetPixel(x, y, grayColorValue);
                        }
                    }

                    var grayFileName = $"gray_{Guid.NewGuid()}.png";
                    var grayFullPath = Path.Combine(imagesPath, grayFileName);
                    string grayWebPath = "/images/" + grayFileName;
                    grayImage.Save(grayFullPath, ImageFormat.Png);
                    HttpContext.Session.SetString("ResultImagePath", grayWebPath);
                }

                // Clear any TempData that might conflict or be redundant
                TempData.Remove("OriginalImage");
                TempData.Remove("ResultImage");
                TempData.Remove("Error");


                return RedirectToAction("Index", "Home");
            }

            TempData["Error"] = "Please upload an image to apply the GrayColor filter.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DefaultColor(IFormFile imageFile, string isNewOriginal)
        {
            // --- Basic Checks ---
            if (_environment == null)
            {
                TempData["Error"] = "Server configuration error.";
                return RedirectToAction("Index", "Home");
            }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            string currentOriginalPath = HttpContext.Session.GetString("OriginalImagePath");

            // --- Logic: Handle New Upload or Reset ---

            // Case 1: A new image is being uploaded via the "Default Color" button.
            // We treat this like any other new upload: save it as the new original and result.
            if (newOriginalRequest && imageFile != null && imageFile.Length > 0)
            {
                var originalSessionFileName = $"original_session_{Guid.NewGuid()}{Path.GetExtension(GetSafeFilename(imageFile.FileName))}";
                var originalSessionFullPath = Path.Combine(imagesPath, originalSessionFileName);
                using (var stream = new FileStream(originalSessionFullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                string newWebPath = "/images/" + originalSessionFileName;
                HttpContext.Session.SetString("OriginalImagePath", newWebPath);
                HttpContext.Session.SetString("ResultImagePath", newWebPath); // New original IS the new result.
                TempData.Remove("Error");
            }
            // Case 2: No new image is uploaded, AND an original already exists in the session.
            // This means the user wants to RESET the filtered image back to the original.
            else if (!string.IsNullOrEmpty(currentOriginalPath))
            {
                // Set the result path to be the same as the original path.
                HttpContext.Session.SetString("ResultImagePath", currentOriginalPath);
                TempData.Remove("Error");
            }
            // Case 3: No new image was uploaded, and no original exists in session.
            // This means the user clicked "Default Color" without uploading anything yet.
            else
            {
                TempData["Error"] = "Please upload an image first before resetting.";
            }

            // --- Redirect ---
            return RedirectToAction("Index", "Home");
        }





        // Add this to your FiltersController class if it's not already there
        // private static readonly Random _random = new Random();

        [HttpPost]
        public async Task<IActionResult> SaltAndPepperNoise(IFormFile imageFile, string isNewOriginal)
        {
            if (_environment == null)
            {
                TempData["Error"] = "Server configuration error: Web host environment not available.";
                return RedirectToAction("Index", "Home");
            }
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Please upload an image to apply Salt & Pepper noise.";
                return RedirectToAction("Index", "Home");
            }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }

            // --- Handle OriginalImagePath in Session ---
            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                var originalSessionFileName = $"original_session_{Guid.NewGuid()}{Path.GetExtension(GetSafeFilename(imageFile.FileName))}";
                var originalSessionFullPath = Path.Combine(imagesPath, originalSessionFileName);
                using (var stream = new FileStream(originalSessionFullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                HttpContext.Session.SetString("OriginalImagePath", "/images/" + originalSessionFileName);
            }

            // --- Apply Filter to the input imageFile ---
            Bitmap bitmapToFilter;
            try
            {
                using (var stream = imageFile.OpenReadStream())
                {
                    stream.Position = 0; // ✅ Set position on the stream, not imageFile
                    bitmapToFilter = new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading image for filtering: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }

            using (var noisyBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height, bitmapToFilter.PixelFormat))
            using (Graphics g = Graphics.FromImage(noisyBitmap))
            {
                g.DrawImage(bitmapToFilter, 0, 0);

                double saltProb = 0.05;
                double pepperProb = 0.05;

                for (int y = 0; y < noisyBitmap.Height; y++)
                {
                    for (int x = 0; x < noisyBitmap.Width; x++)
                    {
                        Color originalPixel = noisyBitmap.GetPixel(x, y);
                        double randValue = _random.NextDouble();

                        if (randValue < pepperProb)
                        {
                            noisyBitmap.SetPixel(x, y, Color.FromArgb(originalPixel.A, 0, 0, 0));
                        }
                        else if (randValue < pepperProb + saltProb)
                        {
                            noisyBitmap.SetPixel(x, y, Color.FromArgb(originalPixel.A, 255, 255, 255));
                        }
                    }
                }

                var noisyFileName = $"noise_saltpepper_{Guid.NewGuid()}.png";
                var noisyFullPath = Path.Combine(imagesPath, noisyFileName);
                noisyBitmap.Save(noisyFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + noisyFileName);
            }

            bitmapToFilter.Dispose();
            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }



        /// <summary>
        /// Generates a random number from a Gaussian distribution using the Box-Muller transform.
        /// </summary>
        /// <param name="mean">The mean of the distribution.</param>
        /// <param name="stdDev">The standard deviation of the distribution.</param>
        /// <returns>A Gaussian-distributed random number.</returns>
        private double GetGaussianRandom(double mean, double stdDev)
        {
            // Use Box-Muller transform
            if (!double.IsNaN(_spareGaussian))
            {
                double val = _spareGaussian;
                _spareGaussian = double.NaN;
                return mean + stdDev * val;
            }

            double u1, u2, s;
            do
            {
                u1 = 2.0 * _random.NextDouble() - 1.0;
                u2 = 2.0 * _random.NextDouble() - 1.0;
                s = u1 * u1 + u2 * u2;
            } while (s >= 1.0 || s == 0); // Ensure s is in (0, 1)

            double mul = Math.Sqrt(-2.0 * Math.Log(s) / s);

            _spareGaussian = u2 * mul; // Store one for next time
            double randStdNormal = u1 * mul; // Use the other one now

            return mean + stdDev * randStdNormal;
        }

        /// <summary>
        /// Generates a random number from a Poisson distribution.
        /// </summary>
        /// <param name="mean">The mean (lambda) of the distribution.</param>
        /// <returns>A Poisson-distributed random number.</returns>
        private int GetPoissonRandom(double mean)
        {
            if (mean <= 0) return 0;
            // Algorithm by Donald Knuth
            double L = Math.Exp(-mean);
            int k = 0;
            double p = 1.0;
            do
            {
                k++;
                p *= _random.NextDouble();
            } while (p > L);
            return k - 1;
        }

        [HttpPost]
        public async Task<IActionResult> GaussianNoise(IFormFile imageFile, string isNewOriginal)
        {
            if (_environment == null)
            {
                TempData["Error"] = "Server configuration error.";
                return RedirectToAction("Index", "Home");
            }
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Please upload an image to apply Gaussian noise.";
                return RedirectToAction("Index", "Home");
            }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            // Handle OriginalImagePath in Session
            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                var originalSessionFileName = $"original_session_{Guid.NewGuid()}{Path.GetExtension(GetSafeFilename(imageFile.FileName))}";
                var originalSessionFullPath = Path.Combine(imagesPath, originalSessionFileName);
                using (var stream = new FileStream(originalSessionFullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                HttpContext.Session.SetString("OriginalImagePath", "/images/" + originalSessionFileName);
            }

            Bitmap bitmapToFilter;
            try
            {
                using (var stream = imageFile.OpenReadStream())
                {
                    stream.Position = 0; // ✅ Set position on the stream, not imageFile
                    bitmapToFilter = new Bitmap(stream);
                }

                using (var stream = imageFile.OpenReadStream())
                {
                    bitmapToFilter = new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading image: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }

            using (var noisyBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height, bitmapToFilter.PixelFormat))
            using (Graphics g = Graphics.FromImage(noisyBitmap))
            {
                g.DrawImage(bitmapToFilter, 0, 0); // Start with a copy

                double mean = 0;
                double stdDev = 25; // Adjust this value to control noise strength

                for (int y = 0; y < noisyBitmap.Height; y++)
                {
                    for (int x = 0; x < noisyBitmap.Width; x++)
                    {
                        Color currentPixel = noisyBitmap.GetPixel(x, y);
                        int noiseVal = (int)Math.Round(GetGaussianRandom(mean, stdDev));

                        int r = Math.Max(0, Math.Min(255, currentPixel.R + noiseVal));
                        int gr = Math.Max(0, Math.Min(255, currentPixel.G + noiseVal)); // 'g' is already used by Graphics
                        int b = Math.Max(0, Math.Min(255, currentPixel.B + noiseVal));

                        noisyBitmap.SetPixel(x, y, Color.FromArgb(currentPixel.A, r, gr, b));
                    }
                }

                var noisyFileName = $"noise_gaussian_{Guid.NewGuid()}.png";
                var noisyFullPath = Path.Combine(imagesPath, noisyFileName);
                noisyBitmap.Save(noisyFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + noisyFileName);
            }
            bitmapToFilter.Dispose();

            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }




        [HttpPost]
        public async Task<IActionResult> PoissonNoise(IFormFile imageFile, string isNewOriginal)
        {
            if (_environment == null)
            {
                TempData["Error"] = "Server configuration error.";
                return RedirectToAction("Index", "Home");
            }
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Please upload an image to apply Poisson noise.";
                return RedirectToAction("Index", "Home");
            }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            // Handle OriginalImagePath in Session
            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                var originalSessionFileName = $"original_session_{Guid.NewGuid()}{Path.GetExtension(GetSafeFilename(imageFile.FileName))}";
                var originalSessionFullPath = Path.Combine(imagesPath, originalSessionFileName);
                using (var stream = new FileStream(originalSessionFullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }
                HttpContext.Session.SetString("OriginalImagePath", "/images/" + originalSessionFileName);
            }

            Bitmap bitmapToFilter;
            try
            {
                using (var stream = imageFile.OpenReadStream())
                {
                    stream.Position = 0; // ✅ Set position on the stream, not imageFile
                    bitmapToFilter = new Bitmap(stream);
                }
                using (var stream = imageFile.OpenReadStream())
                {
                    bitmapToFilter = new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading image: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }

            using (var noisyBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height, bitmapToFilter.PixelFormat))
            using (Graphics g = Graphics.FromImage(noisyBitmap))
            {
                g.DrawImage(bitmapToFilter, 0, 0); // Start with a copy

                for (int y = 0; y < noisyBitmap.Height; y++)
                {
                    for (int x = 0; x < noisyBitmap.Width; x++)
                    {
                        Color currentPixel = noisyBitmap.GetPixel(x, y);
                        int noisyR = Math.Min(255, GetPoissonRandom(currentPixel.R));
                        int noisyG = Math.Min(255, GetPoissonRandom(currentPixel.G));
                        int noisyB = Math.Min(255, GetPoissonRandom(currentPixel.B));
                        noisyBitmap.SetPixel(x, y, Color.FromArgb(currentPixel.A, noisyR, noisyG, noisyB));
                    }
                }

                var noisyFileName = $"noise_poisson_{Guid.NewGuid()}.png";
                var noisyFullPath = Path.Combine(imagesPath, noisyFileName);
                noisyBitmap.Save(noisyFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + noisyFileName);
            }
            bitmapToFilter.Dispose();

            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }

    }
}