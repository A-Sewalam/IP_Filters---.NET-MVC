
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;




namespace IP_Filters.Controllers
{
    public class FiltersController : Controller
    {


      //                                                         *************initial setup**************

      
    
        private readonly IWebHostEnvironment _environment;
        private static readonly Random _random = new Random();
        private double _spareGaussian = double.NaN;

        public FiltersController(IWebHostEnvironment environment)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        private string GetSafeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return "untitled.png";
            var nameOnly = Path.GetFileName(filename);
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                nameOnly = nameOnly.Replace(c, '_');
            }
            return string.IsNullOrEmpty(nameOnly) ? "untitled.png" : nameOnly;
        }

        private void SaveOriginalToSession(IFormFile imageFile, string imagesPath)
        {
            var originalSessionFileName = $"original_session_{Guid.NewGuid()}{Path.GetExtension(GetSafeFilename(imageFile.FileName))}";
            var originalSessionFullPath = Path.Combine(imagesPath, originalSessionFileName);
            using (var stream = new FileStream(originalSessionFullPath, FileMode.Create))
            {
                imageFile.CopyTo(stream);
            }
            HttpContext.Session.SetString("OriginalImagePath", "/images/" + originalSessionFileName);
        }

        private Bitmap LoadBitmapFromFormFile(IFormFile imageFile)
        {
         
            using (var stream = imageFile.OpenReadStream())
            {
                return new Bitmap(stream);
            }
        }


//                                                         *************default image method**************


        [HttpPost]
        public IActionResult DefaultColor(IFormFile imageFile, string isNewOriginal)
        {
            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            string currentOriginalPathInSession = HttpContext.Session.GetString("OriginalImagePath");

            if (newOriginalRequest && imageFile != null && imageFile.Length > 0)
            {
                SaveOriginalToSession(imageFile, imagesPath);
                HttpContext.Session.SetString("ResultImagePath", HttpContext.Session.GetString("OriginalImagePath"));
                TempData.Remove("Error");
            }
            else if (!string.IsNullOrEmpty(currentOriginalPathInSession))
            {
                HttpContext.Session.SetString("ResultImagePath", currentOriginalPathInSession);
                TempData.Remove("Error");
            }
            else
            {
                TempData["Error"] = "Please upload an image first.";
            }
            return RedirectToAction("Index", "Home");
        }


//                                                         *************default image method**************
        

        [HttpPost]
        public IActionResult GrayColor(IFormFile imageFile, string isNewOriginal)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                SaveOriginalToSession(imageFile, imagesPath);
            }

            Bitmap bitmapToFilter = null;
            try
            {
                bitmapToFilter = LoadBitmapFromFormFile(imageFile);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading image: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }

            using (bitmapToFilter)
            using (var processedBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height))
            {
                for (int y = 0; y < bitmapToFilter.Height; y++)
                {
                    for (int x = 0; x < bitmapToFilter.Width; x++)
                    {
                        var pixel = bitmapToFilter.GetPixel(x, y);
                        int grayVal = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                        processedBitmap.SetPixel(x, y, Color.FromArgb(pixel.A, grayVal, grayVal, grayVal));
                    }
                }
                var processedFileName = $"gray_{Guid.NewGuid()}.png";
                var processedFullPath = Path.Combine(imagesPath, processedFileName);
                processedBitmap.Save(processedFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + processedFileName);
            }
            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }


        //                                                         *************default image method**************
        

        [HttpPost]
        public IActionResult SaltAndPepperNoise(IFormFile imageFile, string isNewOriginal)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                SaveOriginalToSession(imageFile, imagesPath);
            }

            Bitmap bitmapToFilter = null;
            try { bitmapToFilter = LoadBitmapFromFormFile(imageFile); }
            catch (Exception ex) { TempData["Error"] = $"Error loading image: {ex.Message}"; return RedirectToAction("Index", "Home"); }

            using (bitmapToFilter)
            using (var noisyBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height, bitmapToFilter.PixelFormat))
            using (Graphics g = Graphics.FromImage(noisyBitmap))
            {
                g.DrawImage(bitmapToFilter, 0, 0); 
                double saltProb = 0.05; double pepperProb = 0.05;
                for (int y = 0; y < noisyBitmap.Height; y++)
                {
                    for (int x = 0; x < noisyBitmap.Width; x++)
                    {
                        Color originalPixel = noisyBitmap.GetPixel(x, y);
                        double randValue = _random.NextDouble();
                        if (randValue < pepperProb) noisyBitmap.SetPixel(x, y, Color.FromArgb(originalPixel.A, 0, 0, 0));
                        else if (randValue < pepperProb + saltProb) noisyBitmap.SetPixel(x, y, Color.FromArgb(originalPixel.A, 255, 255, 255));
                       
                    }
                }
                var noisyFileName = $"noise_saltpepper_{Guid.NewGuid()}.png";
                var noisyFullPath = Path.Combine(imagesPath, noisyFileName);
                noisyBitmap.Save(noisyFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + noisyFileName);
            }
            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }

        private double GetGaussianRandom(double mean, double stdDev)
        {
            if (!double.IsNaN(_spareGaussian)) { double val = _spareGaussian; _spareGaussian = double.NaN; return mean + stdDev * val; }
            double u1, u2, s;
            do { u1 = 2.0 * _random.NextDouble() - 1.0; u2 = 2.0 * _random.NextDouble() - 1.0; s = u1 * u1 + u2 * u2; } while (s >= 1.0 || s == 0);
            double mul = Math.Sqrt(-2.0 * Math.Log(s) / s);
            _spareGaussian = u2 * mul; return mean + stdDev * (u1 * mul);
        }
        private int GetPoissonRandom(double mean)
        {
            if (mean <= 0) return 0; double L = Math.Exp(-mean); int k = 0; double p = 1.0;
            do { k++; p *= _random.NextDouble(); } while (p > L); return k - 1;
        }


        //                                                         *************default image method**************

        

        [HttpPost]
        public IActionResult GaussianNoise(IFormFile imageFile, string isNewOriginal)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                SaveOriginalToSession(imageFile, imagesPath);
            }

            Bitmap bitmapToFilter = null;
            try { bitmapToFilter = LoadBitmapFromFormFile(imageFile); }
            catch (Exception ex) { TempData["Error"] = $"Error loading image: {ex.Message}"; return RedirectToAction("Index", "Home"); }

            using (bitmapToFilter)
            using (var noisyBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height, bitmapToFilter.PixelFormat))
            using (Graphics g = Graphics.FromImage(noisyBitmap))
            {
                g.DrawImage(bitmapToFilter, 0, 0);
                double mean = 0; double stdDev = 25;
                for (int y = 0; y < noisyBitmap.Height; y++)
                {
                    for (int x = 0; x < noisyBitmap.Width; x++)
                    {
                        Color currentPixel = noisyBitmap.GetPixel(x, y); // Get from the copied bitmap
                        int noiseVal = (int)Math.Round(GetGaussianRandom(mean, stdDev));
                        int r = Math.Max(0, Math.Min(255, currentPixel.R + noiseVal));
                        int grVal = Math.Max(0, Math.Min(255, currentPixel.G + noiseVal));
                        int b = Math.Max(0, Math.Min(255, currentPixel.B + noiseVal));
                        noisyBitmap.SetPixel(x, y, Color.FromArgb(currentPixel.A, r, grVal, b));
                    }
                }
                var noisyFileName = $"noise_gaussian_{Guid.NewGuid()}.png";
                var noisyFullPath = Path.Combine(imagesPath, noisyFileName);
                noisyBitmap.Save(noisyFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + noisyFileName);
            }
            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }


        //                                                         *************default image method**************



        [HttpPost]
        public IActionResult PoissonNoise(IFormFile imageFile, string isNewOriginal)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                SaveOriginalToSession(imageFile, imagesPath);
            }

            Bitmap bitmapToFilter = null;
            try { bitmapToFilter = LoadBitmapFromFormFile(imageFile); }
            catch (Exception ex) { TempData["Error"] = $"Error loading image: {ex.Message}"; return RedirectToAction("Index", "Home"); }

            using (bitmapToFilter)
            using (var noisyBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height, bitmapToFilter.PixelFormat))
            using (Graphics g = Graphics.FromImage(noisyBitmap))
            {
                g.DrawImage(bitmapToFilter, 0, 0);
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
            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }

        //                                                         *************default image method**************

        

        [HttpPost]
        public IActionResult AdjustBrightness(IFormFile imageFile, string isNewOriginal, int brightness)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                SaveOriginalToSession(imageFile, imagesPath);
            }

            Bitmap bitmapToFilter = null;
            try { bitmapToFilter = LoadBitmapFromFormFile(imageFile); }
            catch (Exception ex) { TempData["Error"] = $"Error loading image: {ex.Message}"; return RedirectToAction("Index", "Home"); }

            using (bitmapToFilter)
            using (var processedBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height, bitmapToFilter.PixelFormat))
            {
                for (int y = 0; y < bitmapToFilter.Height; y++)
                {
                    for (int x = 0; x < bitmapToFilter.Width; x++)
                    {
                        Color originalPixel = bitmapToFilter.GetPixel(x, y);
                        int r = Math.Max(0, Math.Min(255, originalPixel.R + brightness));
                        int gVal = Math.Max(0, Math.Min(255, originalPixel.G + brightness));
                        int b = Math.Max(0, Math.Min(255, originalPixel.B + brightness));
                        processedBitmap.SetPixel(x, y, Color.FromArgb(originalPixel.A, r, gVal, b));
                    }
                }
                var processedFileName = $"brightness_{brightness}_{Guid.NewGuid()}.png";
                var processedFullPath = Path.Combine(imagesPath, processedFileName);
                processedBitmap.Save(processedFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + processedFileName);
            }
            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }

        //                                                         *************default image method**************

        

        [HttpPost]
        public IActionResult AdjustContrast(IFormFile imageFile, string isNewOriginal, double contrast)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                SaveOriginalToSession(imageFile, imagesPath);
            }

            Bitmap bitmapToFilter = null;
            try { bitmapToFilter = LoadBitmapFromFormFile(imageFile); }
            catch (Exception ex) { TempData["Error"] = $"Error loading image: {ex.Message}"; return RedirectToAction("Index", "Home"); }

            using (bitmapToFilter)
            using (var processedBitmap = new Bitmap(bitmapToFilter.Width, bitmapToFilter.Height, bitmapToFilter.PixelFormat))
            {
                if (contrast <= 0) contrast = 0.1;

                for (int y = 0; y < bitmapToFilter.Height; y++)
                {
                    for (int x = 0; x < bitmapToFilter.Width; x++)
                    {
                        Color originalPixel = bitmapToFilter.GetPixel(x, y);
                        int r = (int)Math.Max(0, Math.Min(255, contrast * (originalPixel.R - 128) + 128));
                        int gVal = (int)Math.Max(0, Math.Min(255, contrast * (originalPixel.G - 128) + 128));
                        int b = (int)Math.Max(0, Math.Min(255, contrast * (originalPixel.B - 128) + 128));
                        processedBitmap.SetPixel(x, y, Color.FromArgb(originalPixel.A, r, gVal, b));
                    }
                }
                var processedFileName = $"contrast_{contrast:F1}_{Guid.NewGuid()}.png";
                var processedFullPath = Path.Combine(imagesPath, processedFileName);
                processedBitmap.Save(processedFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + processedFileName);
            }
            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }


//                                                         *************default image method**************



        //s_histogram_helper_methodes
      

        private int[] CalculateGrayscaleHistogram(Bitmap image)
        {
            int[] histogram = new int[256]; 

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    // Convert to grayscale using luminance formula (same as GrayColor filter)
                    int grayValue = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                    grayValue = Math.Max(0, Math.Min(255, grayValue)); // Clamp value
                    histogram[grayValue]++;
                }
            }
            return histogram;
        }
        
        //e_histogram_helper_methodes
        

        [HttpPost]
        public IActionResult ViewHistogram(IFormFile imageFile, string isNewOriginal)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image to view its histogram."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            Bitmap imageToAnalyze = null;
            try
            {
                
                if (newOriginalRequest)
                {
                    SaveOriginalToSession(imageFile, imagesPath);
                    HttpContext.Session.SetString("ResultImagePath", HttpContext.Session.GetString("OriginalImagePath"));
                }
                imageToAnalyze = LoadBitmapFromFormFile(imageFile);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading image for histogram: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }

            using (imageToAnalyze)
            {
                int[] histogramValues = CalculateGrayscaleHistogram(imageToAnalyze);
                HttpContext.Session.SetString("HistogramData", JsonSerializer.Serialize(histogramValues));
                HttpContext.Session.SetString("ShowHistogram", "true");
            }

            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }


        //                                                         *************default image method**************

        

        [HttpPost]
        public IActionResult ApplyHistogramEqualization(IFormFile imageFile, string isNewOriginal)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image to apply histogram equalization."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            Bitmap originalBitmap = null; 
            try
            {
                if (newOriginalRequest)
                {
                    SaveOriginalToSession(imageFile, imagesPath);
                    
                }
                originalBitmap = LoadBitmapFromFormFile(imageFile);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading image for equalization: {ex.Message}";
                if (originalBitmap != null) originalBitmap.Dispose();
                return RedirectToAction("Index", "Home");
            }

            using (originalBitmap)
            using (Bitmap equalizedBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height, originalBitmap.PixelFormat))
            {
                int[] histogram = CalculateGrayscaleHistogram(originalBitmap);
                int totalPixels = originalBitmap.Width * originalBitmap.Height;

                long[] cdf = new long[256];
                cdf[0] = histogram[0];
                for (int i = 1; i < 256; i++)
                {
                    cdf[i] = cdf[i - 1] + histogram[i];
                }

                long cdfMin = 0;
                for (int i = 0; i < 256; ++i)
                {
                    if (cdf[i] > 0)
                    {
                        cdfMin = cdf[i];
                        break;
                    }
                }
                if (cdfMin == 0 && totalPixels > 0) cdfMin = 1; 
                else if (totalPixels == 0)
                { 
                    TempData["Error"] = "Cannot process an empty image.";
                    return RedirectToAction("Index", "Home");
                }


                byte[] lut = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    
                    double denominator = (double)totalPixels - cdfMin;
                    if (denominator <= 0) denominator = 1.0;

                    lut[i] = (byte)Math.Max(0, Math.Min(255, Math.Round(((double)cdf[i] - cdfMin) / denominator * 255.0)));
                }

                for (int y = 0; y < originalBitmap.Height; y++)
                {
                    for (int x = 0; x < originalBitmap.Width; x++)
                    {
                        Color pixel = originalBitmap.GetPixel(x, y);
                        int grayValue = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                        equalizedBitmap.SetPixel(x, y, Color.FromArgb(pixel.A, lut[grayValue], lut[grayValue], lut[grayValue]));
                    }
                }

                var equalizedFileName = $"equalized_{Guid.NewGuid()}.png";
                var equalizedFullPath = Path.Combine(imagesPath, equalizedFileName);
                equalizedBitmap.Save(equalizedFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + equalizedFileName);

                int[] equalizedHistogramValues = CalculateGrayscaleHistogram(equalizedBitmap);
                HttpContext.Session.SetString("HistogramData", JsonSerializer.Serialize(equalizedHistogramValues));
                HttpContext.Session.SetString("ShowHistogram", "true");
            }

            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }


        //                                                         *************initial setup**************

        
        //s_histogram_helper_methodes


        private Bitmap ApplyConvolutionFilter(Bitmap sourceImage, double[,] kernel, double factor = 1.0, int bias = 0, bool grayscaleOutput = false)
        {
            int kernelWidth = kernel.GetLength(1);
            int kernelHeight = kernel.GetLength(0);
            int radiusX = kernelWidth / 2;
            int radiusY = kernelHeight / 2;

            Bitmap resultBitmap = new Bitmap(sourceImage.Width, sourceImage.Height, sourceImage.PixelFormat);

            Bitmap sourceToProcess = sourceImage;
            if (grayscaleOutput) 
            {
                sourceToProcess = new Bitmap(sourceImage.Width, sourceImage.Height);
                using (Graphics g = Graphics.FromImage(sourceToProcess))
                {
                    ColorMatrix colorMatrix = new ColorMatrix(
                        new float[][]
                        {
                            new float[] {.3f, .3f, .3f, 0, 0},
                            new float[] {.59f, .59f, .59f, 0, 0},
                            new float[] {.11f, .11f, .11f, 0, 0},
                            new float[] {0, 0, 0, 1, 0},
                            new float[] {0, 0, 0, 0, 1}
                        });
                    using (ImageAttributes attributes = new ImageAttributes())
                    {
                        attributes.SetColorMatrix(colorMatrix);
                        g.DrawImage(sourceImage, new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
                                    0, 0, sourceImage.Width, sourceImage.Height, GraphicsUnit.Pixel, attributes);
                    }
                }
            }


            for (int y = 0; y < sourceImage.Height; y++)
            {
                for (int x = 0; x < sourceImage.Width; x++)
                {
                    double sumR = 0, sumG = 0, sumB = 0;
                    double sumGray = 0;

                    for (int ky = -radiusY; ky <= radiusY; ky++)
                    {
                        for (int kx = -radiusX; kx <= radiusX; kx++)
                        {
                            int pixelX = Math.Clamp(x + kx, 0, sourceImage.Width - 1);
                            int pixelY = Math.Clamp(y + ky, 0, sourceImage.Height - 1);
                            Color pixel = sourceToProcess.GetPixel(pixelX, pixelY);
                            double kernelVal = kernel[ky + radiusY, kx + radiusX];

                            if (grayscaleOutput)
                            {
                                sumGray += pixel.R * kernelVal;
                            }
                            else
                            {
                                sumR += pixel.R * kernelVal;
                                sumG += pixel.G * kernelVal;
                                sumB += pixel.B * kernelVal;
                            }
                        }
                    }

                    Color originalAlphaPixel = sourceImage.GetPixel(x, y);

                    if (grayscaleOutput)
                    {
                        int gray = Math.Clamp((int)(factor * sumGray + bias), 0, 255);
                        resultBitmap.SetPixel(x, y, Color.FromArgb(originalAlphaPixel.A, gray, gray, gray));
                    }
                    else
                    {
                        int r = Math.Clamp((int)(factor * sumR + bias), 0, 255);
                        int g = Math.Clamp((int)(factor * sumG + bias), 0, 255);
                        int b = Math.Clamp((int)(factor * sumB + bias), 0, 255);
                        resultBitmap.SetPixel(x, y, Color.FromArgb(originalAlphaPixel.A, r, g, b));
                    }
                }
            }
            if (grayscaleOutput && sourceToProcess != sourceImage)
            {
                sourceToProcess.Dispose();
            }
            return resultBitmap;
        }

        private Bitmap ApplyMedianFilter(Bitmap sourceImage, int matrixSize = 3)
        {
            if (matrixSize % 2 == 0) matrixSize++; 
            int radius = matrixSize / 2;
            Bitmap resultBitmap = new Bitmap(sourceImage.Width, sourceImage.Height, sourceImage.PixelFormat);

            List<int> rValues = new List<int>();
            List<int> gValues = new List<int>();
            List<int> bValues = new List<int>();

            for (int y = 0; y < sourceImage.Height; y++)
            {
                for (int x = 0; x < sourceImage.Width; x++)
                {
                    rValues.Clear();
                    gValues.Clear();
                    bValues.Clear();

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int pixelX = Math.Clamp(x + kx, 0, sourceImage.Width - 1);
                            int pixelY = Math.Clamp(y + ky, 0, sourceImage.Height - 1);
                            Color pixel = sourceImage.GetPixel(pixelX, pixelY);
                            rValues.Add(pixel.R);
                            gValues.Add(pixel.G);
                            bValues.Add(pixel.B);
                        }
                    }
                    rValues.Sort();
                    gValues.Sort();
                    bValues.Sort();

                    Color originalPixel = sourceImage.GetPixel(x, y); // For alpha
                    resultBitmap.SetPixel(x, y, Color.FromArgb(
                        originalPixel.A,
                        rValues[rValues.Count / 2],
                        gValues[gValues.Count / 2],
                        bValues[bValues.Count / 2]
                    ));
                }
            }
            return resultBitmap;
        }
        

        private double[,] GenerateGaussianKernel(int size, double sigma)
        {
            if (size % 2 == 0) size++; 
            double[,] kernel = new double[size, size];
            double sum = 0;
            int radius = size / 2;

            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    double value = (1.0 / (2.0 * Math.PI * sigma * sigma)) * Math.Exp(-(x * x + y * y) / (2.0 * sigma * sigma));
                    kernel[y + radius, x + radius] = value;
                    sum += value;
                }
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[y, x] /= sum;
                }
            }
            return kernel;
        }



        private IActionResult ProcessImageWithFilter(IFormFile imageFile, string isNewOriginal, Func<Bitmap, Bitmap> filterFunction, string filterNamePrefix)
        {
            if (imageFile == null || imageFile.Length == 0) { TempData["Error"] = "Please upload an image."; return RedirectToAction("Index", "Home"); }

            bool newOriginalRequest = Convert.ToBoolean(isNewOriginal);
            var imagesPath = Path.Combine(_environment.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            if (newOriginalRequest || string.IsNullOrEmpty(HttpContext.Session.GetString("OriginalImagePath")))
            {
                SaveOriginalToSession(imageFile, imagesPath);
            }

            Bitmap bitmapToFilter = null;
            try { bitmapToFilter = LoadBitmapFromFormFile(imageFile); }
            catch (Exception ex) { TempData["Error"] = $"Error loading image: {ex.Message}"; return RedirectToAction("Index", "Home"); }

            using (bitmapToFilter)
            using (var processedBitmap = filterFunction(bitmapToFilter))
            {
                var processedFileName = $"{filterNamePrefix}_{Guid.NewGuid()}.png";
                var processedFullPath = Path.Combine(imagesPath, processedFileName);
                processedBitmap.Save(processedFullPath, ImageFormat.Png);
                HttpContext.Session.SetString("ResultImagePath", "/images/" + processedFileName);
                HttpContext.Session.SetString("ShowHistogram", "false"); 
            }
            TempData.Remove("Error");
            return RedirectToAction("Index", "Home");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyLowPassFilter(IFormFile imageFile, string isNewOriginal)
        {
            double[,] kernel = {
                { 1, 1, 1 },
                { 1, 1, 1 },
                { 1, 1, 1 }
            };
            return ProcessImageWithFilter(imageFile, isNewOriginal,
                (bmp) => ApplyConvolutionFilter(bmp, kernel, 1.0 / 9.0), "lowpass");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyAveragingFilter(IFormFile imageFile, string isNewOriginal)
        {
            return ApplyLowPassFilter(imageFile, isNewOriginal); 
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyMedianFilter(IFormFile imageFile, string isNewOriginal)
        {
            return ProcessImageWithFilter(imageFile, isNewOriginal,
                (bmp) => ApplyMedianFilter(bmp, 3), "median");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyGaussianFilter(IFormFile imageFile, string isNewOriginal) 
        {
            double[,] kernel = GenerateGaussianKernel(3, 1.0);
            return ProcessImageWithFilter(imageFile, isNewOriginal,
                (bmp) => ApplyConvolutionFilter(bmp, kernel), "gaussian_blur");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyHighPassFilter(IFormFile imageFile, string isNewOriginal)
        {
            double[,] kernel = {
                { -1, -1, -1 },
                { -1,  9, -1 },
                { -1, -1, -1 }
            };
            return ProcessImageWithFilter(imageFile, isNewOriginal,
                (bmp) => ApplyConvolutionFilter(bmp, kernel, 1.0, 0), "highpass_sharpen");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyLaplacianFilter(IFormFile imageFile, string isNewOriginal)
        {
            double[,] kernel = { 
                { 1,  1,  1 },
                { 1, -8,  1 },
                { 1,  1,  1 }
            };
            return ProcessImageWithFilter(imageFile, isNewOriginal,
                (bmp) => ApplyConvolutionFilter(bmp, kernel, 1.0, 0, grayscaleOutput: true), "laplacian");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyLoGFilter(IFormFile imageFile, string isNewOriginal) 
        {
            double[,] gaussianKernel = GenerateGaussianKernel(3, 1.0); 
            double[,] laplacianKernel = {
                { 0,  1,  0 },
                { 1, -4,  1 },
                { 0,  1,  0 }
            }; 

            return ProcessImageWithFilter(imageFile, isNewOriginal, (bmp) =>
            {
                using (Bitmap blurredBmp = ApplyConvolutionFilter(bmp, gaussianKernel))
                {
                    return ApplyConvolutionFilter(blurredBmp, laplacianKernel, 1.0, 0, grayscaleOutput: true);
                }
            }, "log_filter");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyPrewittFilter(IFormFile imageFile, string isNewOriginal)
        {
            double[,] prewittX = {
                { -1, 0, 1 },
                { -1, 0, 1 },
                { -1, 0, 1 }
            };
            double[,] prewittY = {
                { -1, -1, -1 },
                {  0,  0,  0 },
                {  1,  1,  1 }
            };

            return ProcessImageWithFilter(imageFile, isNewOriginal, (bmp) =>
            {
                Bitmap resultBitmap = new Bitmap(bmp.Width, bmp.Height);
                Bitmap grayscaleBmp = new Bitmap(bmp.Width, bmp.Height);
                using (Graphics g = Graphics.FromImage(grayscaleBmp))
                { // Convert to grayscale for processing
                    ColorMatrix colorMatrix = new ColorMatrix(
                       new float[][] {
                            new float[] {.299f, .299f, .299f, 0, 0},
                            new float[] {.587f, .587f, .587f, 0, 0},
                            new float[] {.114f, .114f, .114f, 0, 0},
                            new float[] {0, 0, 0, 1, 0},
                            new float[] {0, 0, 0, 0, 1}
                       });
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height),
                                0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                }

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        double gx = 0, gy = 0;
                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int ix = Math.Clamp(x + kx, 0, bmp.Width - 1);
                                int iy = Math.Clamp(y + ky, 0, bmp.Height - 1);
                                Color pixel = grayscaleBmp.GetPixel(ix, iy); // Use R channel of grayscale
                                gx += pixel.R * prewittX[ky + 1, kx + 1];
                                gy += pixel.R * prewittY[ky + 1, kx + 1];
                            }
                        }
                        int magnitude = Math.Clamp((int)Math.Sqrt(gx * gx + gy * gy), 0, 255);
                        resultBitmap.SetPixel(x, y, Color.FromArgb(bmp.GetPixel(x, y).A, magnitude, magnitude, magnitude));
                    }
                }
                grayscaleBmp.Dispose();
                return resultBitmap;
            }, "prewitt");
        }



    //                                                         *************initial setup**************
    

        [HttpPost]
        public IActionResult ApplySobelFilter(IFormFile imageFile, string isNewOriginal)
        {
            double[,] sobelX = {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };
            double[,] sobelY = {
                { -1, -2, -1 },
                {  0,  0,  0 },
                {  1,  2,  1 }
            };
            return ProcessImageWithFilter(imageFile, isNewOriginal, (bmp) =>
            {
                Bitmap resultBitmap = new Bitmap(bmp.Width, bmp.Height);
                Bitmap grayscaleBmp = new Bitmap(bmp.Width, bmp.Height);
                using (Graphics g = Graphics.FromImage(grayscaleBmp))
                { // Convert to grayscale for processing
                    ColorMatrix colorMatrix = new ColorMatrix(
                       new float[][] {
                            new float[] {.299f, .299f, .299f, 0, 0},
                            new float[] {.587f, .587f, .587f, 0, 0},
                            new float[] {.114f, .114f, .114f, 0, 0},
                            new float[] {0, 0, 0, 1, 0},
                            new float[] {0, 0, 0, 0, 1}
                       });
                    ImageAttributes attributes = new ImageAttributes();
                    attributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height),
                                0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
                }

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        double gx = 0, gy = 0;
                        for (int ky = -1; ky <= 1; ky++)
                        {
                            for (int kx = -1; kx <= 1; kx++)
                            {
                                int ix = Math.Clamp(x + kx, 0, bmp.Width - 1);
                                int iy = Math.Clamp(y + ky, 0, bmp.Height - 1);
                                Color pixel = grayscaleBmp.GetPixel(ix, iy);
                                gx += pixel.R * sobelX[ky + 1, kx + 1];
                                gy += pixel.R * sobelY[ky + 1, kx + 1];
                            }
                        }
                        int magnitude = Math.Clamp((int)Math.Sqrt(gx * gx + gy * gy), 0, 255);
                        resultBitmap.SetPixel(x, y, Color.FromArgb(bmp.GetPixel(x, y).A, magnitude, magnitude, magnitude));
                    }
                }
                grayscaleBmp.Dispose();
                return resultBitmap;
            }, "sobel");
        }


        private Bitmap BinarizeImage(Bitmap source, int threshold = 128)
        {
            Bitmap binaryBitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format1bppIndexed);

            Bitmap tempBitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(tempBitmap))
            {
                g.DrawImage(source, 0, 0);
            }

            for (int y = 0; y < tempBitmap.Height; y++)
            {
                for (int x = 0; x < tempBitmap.Width; x++)
                {
                    Color c = tempBitmap.GetPixel(x, y);
                    int gray = (int)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
                    tempBitmap.SetPixel(x, y, gray < threshold ? Color.Black : Color.White);
                }
            }


            Bitmap outputBinaryLook = new Bitmap(source.Width, source.Height, source.PixelFormat);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color c = source.GetPixel(x, y);
                    int gray = (int)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
                    outputBinaryLook.SetPixel(x, y, gray < threshold ? Color.Black : Color.White);
                }
            }
            tempBitmap.Dispose();
            return outputBinaryLook;
        }

        private Bitmap ApplyMorphologicalOperation(Bitmap binarySource, bool isDilation, int kernelSize = 3)
        {
            Bitmap resultBitmap = new Bitmap(binarySource.Width, binarySource.Height, binarySource.PixelFormat);
            int radius = kernelSize / 2;

            for (int y = 0; y < binarySource.Height; y++)
            {
                for (int x = 0; x < binarySource.Width; x++)
                {
                    bool setNewPixelToWhite = isDilation ? false : true; // Dilation: if any neighbor is white -> white. Erosion: if any neighbor is black -> black.

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            int nx = Math.Clamp(x + kx, 0, binarySource.Width - 1);
                            int ny = Math.Clamp(y + ky, 0, binarySource.Height - 1);
                            Color neighborPixel = binarySource.GetPixel(nx, ny);
                            bool isNeighborWhite = neighborPixel.R == 255; // Assuming R=G=B for white

                            if (isDilation && isNeighborWhite)
                            {
                                setNewPixelToWhite = true;
                                break;
                            }
                            if (!isDilation && !isNeighborWhite) // Erosion: if a neighbor is black
                            {
                                setNewPixelToWhite = false;
                                break;
                            }
                        }
                        if ((isDilation && setNewPixelToWhite) || (!isDilation && !setNewPixelToWhite && ky == radius)) break; // Optimization
                    }
                    resultBitmap.SetPixel(x, y, setNewPixelToWhite ? Color.White : Color.Black);
                }
            }
            return resultBitmap;
        }


        //                                                         *************initial setup**************


        
        [HttpPost]
        public IActionResult ApplyDilation(IFormFile imageFile, string isNewOriginal)
        {
            return ProcessImageWithFilter(imageFile, isNewOriginal, (bmp) =>
            {
                using (Bitmap binaryBmp = BinarizeImage(bmp))
                {
                    return ApplyMorphologicalOperation(binaryBmp, true);
                }
            }, "dilation");
        }
        

        //                                                         *************initial setup**************
        

        [HttpPost]
        public IActionResult ApplyErosion(IFormFile imageFile, string isNewOriginal)
        {
            return ProcessImageWithFilter(imageFile, isNewOriginal, (bmp) =>
            {
                using (Bitmap binaryBmp = BinarizeImage(bmp))
                {
                    return ApplyMorphologicalOperation(binaryBmp, false);
                }
            }, "erosion");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyOpen(IFormFile imageFile, string isNewOriginal) // Erosion then Dilation
        {
            return ProcessImageWithFilter(imageFile, isNewOriginal, (bmp) =>
            {
                using (Bitmap binaryBmp = BinarizeImage(bmp))
                using (Bitmap erodedBmp = ApplyMorphologicalOperation(binaryBmp, false))
                {
                    return ApplyMorphologicalOperation(erodedBmp, true);
                }
            }, "open_morph");
        }

        //                                                         *************initial setup**************

        [HttpPost]
        public IActionResult ApplyClose(IFormFile imageFile, string isNewOriginal) // Dilation then Erosion
        {
            return ProcessImageWithFilter(imageFile, isNewOriginal, (bmp) =>
            {
                using (Bitmap binaryBmp = BinarizeImage(bmp))
                using (Bitmap dilatedBmp = ApplyMorphologicalOperation(binaryBmp, true))
                {
                    return ApplyMorphologicalOperation(dilatedBmp, false);
                }
            }, "close_morph");
        }
        private Bitmap ConvertToGrayscale(Bitmap original)
        {
            Bitmap grayscale = new Bitmap(original.Width, original.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(grayscale))
            {
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                    {
                        new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                        new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                        new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                        new float[] {0, 0, 0, 1, 0},
                        new float[] {0, 0, 0, 0, 1}
                    });

                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(colorMatrix);
                    g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            ColorPalette cp = grayscale.Palette;
            if (cp.Entries.Length >= 256)
            {
                for (int i = 0; i < 256; i++)
                    cp.Entries[i] = Color.FromArgb(255, i, i, i);
                grayscale.Palette = cp;
            }

            return grayscale;
        }

        private Bitmap ApplySobelAndThreshold(Bitmap grayscaleBitmap, byte sobelThreshold)
        {
            Bitmap edgeBitmap = new Bitmap(grayscaleBitmap.Width, grayscaleBitmap.Height, PixelFormat.Format8bppIndexed);
            ColorPalette cp = edgeBitmap.Palette;
            for (int i = 0; i < 256; i++) cp.Entries[i] = Color.FromArgb(255, i, i, i);
            edgeBitmap.Palette = cp;

            double[,] sobelXKernel = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            double[,] sobelYKernel = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            BitmapData grayData = null;
            BitmapData edgeData = null;

            try
            {
                grayData = grayscaleBitmap.LockBits(new Rectangle(0, 0, grayscaleBitmap.Width, grayscaleBitmap.Height),
                                                    ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                edgeData = edgeBitmap.LockBits(new Rectangle(0, 0, edgeBitmap.Width, edgeBitmap.Height),
                                                ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                int width = grayscaleBitmap.Width;
                int height = grayscaleBitmap.Height;
                int stride = grayData.Stride; // Assuming stride is same for grayData and edgeData

                unsafe
                {
                    byte* pGray = (byte*)grayData.Scan0;
                    byte* pEdge = (byte*)edgeData.Scan0;

                    // Process inner pixels with Sobel kernel
                    for (int y = 1; y < height - 1; y++)
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            double gx = 0;
                            double gy = 0;
                            for (int ky = -1; ky <= 1; ky++)
                            {
                                for (int kx = -1; kx <= 1; kx++)
                                {
                                    byte pixelVal = pGray[(y + ky) * stride + (x + kx)];
                                    gx += sobelXKernel[ky + 1, kx + 1] * pixelVal;
                                    gy += sobelYKernel[ky + 1, kx + 1] * pixelVal;
                                }
                            }
                            double magnitude = Math.Sqrt(gx * gx + gy * gy);
                            pEdge[y * stride + x] = (magnitude > sobelThreshold) ? (byte)255 : (byte)0; // White for edge, black for non-edge
                        }
                    }

                    for (int y = 0; y < height; y++)
                    {
                        if (y == 0 || y == height - 1) 
                        {
                            for (int x = 0; x < width; x++) pEdge[y * stride + x] = 0;
                        }
                        else 
                        {
                            pEdge[y * stride] = 0;
                            pEdge[y * stride + width - 1] = 0;
                        }
                    }
                }
            }
            finally
            {
                if (grayData != null) grayscaleBitmap.UnlockBits(grayData);
                if (edgeData != null) edgeBitmap.UnlockBits(edgeData);
            }
            return edgeBitmap;
        }


        //                                                         *************initial setup**************


        [HttpPost]
        public IActionResult DetectLinesHough(IFormFile imageFile, string isNewOriginal)
        {
            return ProcessImageWithFilter(imageFile, isNewOriginal, (originalBmp) =>
            {
                int width = originalBmp.Width;
                int height = originalBmp.Height;

                // Convert to grayscale
                Bitmap gray = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(gray))
                {
                    ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                    {
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
                    });

                    using (ImageAttributes attributes = new ImageAttributes())
                    {
                        attributes.SetColorMatrix(colorMatrix);
                        g.DrawImage(originalBmp, new Rectangle(0, 0, width, height),
                            0, 0, width, height, GraphicsUnit.Pixel, attributes);
                    }
                }

                Bitmap edge = ApplySobelAndThreshold(gray, 100);

                int maxRho = (int)Math.Ceiling(Math.Sqrt(width * width + height * height));
                int rhoRange = 2 * maxRho + 1;
                int thetaSteps = 180;

                int[,] accumulator = new int[thetaSteps, rhoRange];
                double[] cosTable = new double[thetaSteps];
                double[] sinTable = new double[thetaSteps];

                for (int theta = 0; theta < thetaSteps; theta++)
                {
                    double rad = theta * Math.PI / 180;
                    cosTable[theta] = Math.Cos(rad);
                    sinTable[theta] = Math.Sin(rad);
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color pixel = edge.GetPixel(x, y);
                        if (pixel.R == 255) 
                        {
                            for (int theta = 0; theta < thetaSteps; theta++)
                            {
                                double rho = x * cosTable[theta] + y * sinTable[theta];
                                int rhoIndex = (int)Math.Round(rho) + maxRho;
                                if (rhoIndex >= 0 && rhoIndex < rhoRange)
                                    accumulator[theta, rhoIndex]++;
                            }
                        }
                    }
                }

                int threshold = 350; 
                Bitmap result = new Bitmap(originalBmp);
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(Color.Red, 2))
                    {
                        for (int theta = 0; theta < thetaSteps; theta++)
                        {
                            for (int rhoIndex = 0; rhoIndex < rhoRange; rhoIndex++)
                            {
                                if (accumulator[theta, rhoIndex] > threshold)
                                {
                                    double rho = rhoIndex - maxRho;
                                    double cosTheta = cosTable[theta];
                                    double sinTheta = sinTable[theta];
                                    Point pt1 = new Point();
                                    Point pt2 = new Point();

                                    if (sinTheta != 0)
                                    {
                                        pt1.X = 0;
                                        pt1.Y = (int)((rho - pt1.X * cosTheta) / sinTheta);
                                        pt2.X = width;
                                        pt2.Y = (int)((rho - pt2.X * cosTheta) / sinTheta);
                                    }
                                    else
                                    {
                                        pt1.Y = 0;
                                        pt1.X = (int)(rho / cosTheta);
                                        pt2.Y = height;
                                        pt2.X = (int)(rho / cosTheta);
                                    }

                                    g.DrawLine(pen, pt1, pt2);
                                }
                            }
                        }
                    }
                }

                gray.Dispose();
                edge.Dispose();
                return result;
            }, "hough_line");
        }


        //                                                         *************initial setup**************


        [HttpPost]
        public IActionResult DetectCirclesHough(IFormFile imageFile, string isNewOriginal)
        {
            return ProcessImageWithFilter(imageFile, isNewOriginal, (originalBmp) =>
            {
                int radius = 30;            
                int threshold = 120;         
                int angleStep = 2;           

                int width = originalBmp.Width;
                int height = originalBmp.Height;

                Bitmap gray = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(gray))
                {
                    ColorMatrix colorMatrix = new ColorMatrix(new float[][]
                    {
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
                    });

                    using (ImageAttributes attributes = new ImageAttributes())
                    {
                        attributes.SetColorMatrix(colorMatrix);
                        g.DrawImage(originalBmp, new Rectangle(0, 0, width, height),
                            0, 0, width, height, GraphicsUnit.Pixel, attributes);
                    }
                }

                Bitmap edgeBmp = ApplySobelAndThreshold(gray, 100);

                int[,] accumulator = new int[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color c = edgeBmp.GetPixel(x, y);
                        if (c.R == 255)
                        {
                            for (int angle = 0; angle < 360; angle += angleStep)
                            {
                                double theta = angle * Math.PI / 180;
                                int a = (int)Math.Round(x - radius * Math.Cos(theta));
                                int b = (int)Math.Round(y - radius * Math.Sin(theta));
                                if (a >= 0 && a < width && b >= 0 && b < height)
                                {
                                    accumulator[a, b]++;
                                }
                            }
                        }
                    }
                }

                Bitmap result = new Bitmap(originalBmp);
                using (Graphics g = Graphics.FromImage(result))
                {
                    using (Pen pen = new Pen(Color.Blue, 2))
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                if (accumulator[x, y] >= threshold)
                                {
                                    g.DrawEllipse(pen, x - radius, y - radius, 2 * radius, 2 * radius);
                                }
                            }
                        }
                    }
                }

                gray.Dispose();
                edgeBmp.Dispose();
                return result;
            }, "hough_circle");
        }




    }
}
