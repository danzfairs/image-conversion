﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ImageConversionApi
{
    [ApiController]
    public class ImageConversionController : ControllerBase
    {
        private readonly ILogger<ImageConversionController> _logger;

        public ImageConversionController(ILogger<ImageConversionController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Takes a pdf and returns a zip file containing a jpeg for each page of the PDF.
        /// </summary>
        /// <returns></returns>
        [HttpPost("image/tiff-to-pdf")]
        public ActionResult<FileStreamResult> ConvertPdfToJpegs(IFormFile file)
        {
            var inputFilePath = string.Empty;
            var outputFilePath = string.Empty;
            try
            {
                if ((file?.Length ?? 0) == 0)
                {
                    return BadRequest("No file was uploaded or the file had no content.");
                }

                var scanFileStream = new MemoryStream();
                file.CopyTo(scanFileStream);

                // if (IsPdf(file.FileName, scanFileStream))
                // {
                //     return BadRequest("You must upload a pdf file.");
                // }

                var guid = Guid.NewGuid();

                inputFilePath = Path.Combine(@$"C:\temp\tempImageFiles\{guid}\input", file.FileName);


                var jpegPaths = ConvertTiffToJpegs(file, scanFileStream, inputFilePath);

                // if (!IsVirusClean(inputFilePath))
                // {
                //     return BadRequest("Virus detected in uploaded file.");
                // }

                var ghostscriptPath = @"C:\Program Files (x86)\gs\gs9.06\bin\gswin32c.exe";
                outputFilePath = Path.Combine($@"C:\temp\tempImageFiles\{guid}\output", Path.ChangeExtension(file.FileName, "tiff"));

                var cmd =
                    $"-dNOPAUSE -q -sDEVICE=tiff24nc -r500 -dBATCH -sOutputFile=\"{outputFilePath}\" \"{inputFilePath}\"";

                var myProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = ghostscriptPath, Arguments = cmd, WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardError = true
                    }
                };


                myProcess.Start();
                var errorMessage = myProcess.StandardError.ReadToEnd();

                myProcess.WaitForExit();

                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                }

                var outputFileBytes = System.IO.File.ReadAllBytes(outputFilePath);
                var outputStream = new MemoryStream(outputFileBytes);



                return File(outputStream, "image/tiff");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(inputFilePath))
                {
                    System.IO.File.Delete(inputFilePath);
                }
                if (System.IO.File.Exists(outputFilePath))
                {
                    System.IO.File.Delete(outputFilePath);
                }
            }
        }

        /// <summary>
        /// Converts a tiff to jpegs, returns the file names of the created files.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="scanFileStream"></param>
        /// <param name="inputFilePath"></param>
        /// <returns></returns>
        private static List<string> ConvertTiffToJpegs(IFormFile file, MemoryStream scanFileStream, string inputFilePath)
        {
            var jpegPaths = new List<string>();
            using (var image = Image.FromStream(scanFileStream))
            {
                var frameDimension = new FrameDimension(image.FrameDimensionsList[0]);
                var pageCount = image.GetFrameCount(frameDimension);
                for (var pageNumber = 0; pageNumber < pageCount; pageNumber++)
                {
                    image.SelectActiveFrame(frameDimension, pageNumber);
                    using (var bmp = new Bitmap(image))
                    {
                        var jpegPath = @$"{inputFilePath}\{Path.GetFileNameWithoutExtension(file.FileName)}{pageNumber}.jpg";
                        jpegPaths.Add(jpegPath);
                        bmp.Save(jpegPath, ImageFormat.Jpeg);
                    }
                }
            }

            return jpegPaths;
        }


        [HttpPost("image/pdf-to-tiff")]
        public ActionResult<FileStreamResult> ConvertPdfToTiff(IFormFile file)
        {
            var inputFilePath = string.Empty;
            var outputFilePath = string.Empty;
            try
            {
                if ((file?.Length ?? 0) == 0)
                {
                    //TODO:  Scan the file for viruses, make sure it is a PDF.
                    return BadRequest("No file was uploaded or the file had no content.");
                }

                var scanFileStream = new MemoryStream();
                file.CopyTo(scanFileStream);

                if (IsPdf(file.FileName, scanFileStream))
                {
                    return BadRequest("You must upload a pdf file.");
                }

                var guid = Guid.NewGuid();

                inputFilePath = Path.Combine(@"C:\temp\tempImageFiles", $"{guid}.pdf");

                using var inputFileStream = new FileStream(inputFilePath, FileMode.Create);
                file.CopyTo(inputFileStream);

                // if (!IsVirusClean(inputFilePath))
                // {
                //     return BadRequest("Virus detected in uploaded file.");
                // }

                var ghostscriptPath = @"C:\Program Files (x86)\gs\gs9.06\bin\gswin32c.exe";
                outputFilePath = Path.Combine(@"C:\temp\tempImageFiles", $"{guid}.tiff");

                var cmd =
                    $"-dNOPAUSE -q -sDEVICE=tiff24nc -r500 -dBATCH -sOutputFile=\"{outputFilePath}\" \"{inputFilePath}\"";

                var myProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = ghostscriptPath, Arguments = cmd, WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardError = true
                    }
                };


                myProcess.Start();
                var errorMessage = myProcess.StandardError.ReadToEnd();

                myProcess.WaitForExit();

                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
                }

                var outputFileBytes = System.IO.File.ReadAllBytes(outputFilePath);
                var outputStream = new MemoryStream(outputFileBytes);



                return File(outputStream, "image/tiff");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            finally
            {
                if (System.IO.File.Exists(inputFilePath))
                {
                    System.IO.File.Delete(inputFilePath);
                }
                if (System.IO.File.Exists(outputFilePath))
                {
                    System.IO.File.Delete(outputFilePath);
                }
            }
        }

        private static bool IsPdf(string fileName, string path)
        {
            using var f = System.IO.File.OpenRead(path);
            return IsPdf(fileName, f);
        }

        private static bool IsPdf(string fileName, Stream stream)
        {
            if (!fileName.EndsWith(".pdf"))
            {
                return false;
            }
            var pdfString = "%PDF-";
            var pdfBytes = Encoding.ASCII.GetBytes(pdfString);
            var len = pdfBytes.Length;
            var buf = new byte[len];
            var remaining = len;
            var pos = 0;

            while (remaining > 0)
            {
                var amtRead = stream.Read(buf, pos, remaining);
                if (amtRead == 0) return false;
                remaining -= amtRead;
                pos += amtRead;
            }

            return pdfBytes.SequenceEqual(buf);
        }

        private static bool IsVirusClean(string filePath)
        {
            // if (!AppManager.AppSettings.DoVirusScan) return true;
            var isClean = false;
            var filePathReport = filePath + ".report";
            var myProcess = new Process();
            myProcess.StartInfo.FileName = "myPathToVirusScanner";//AppManager.AppSettings.VirusScanPath;
            //myProcess.StartInfo.Arguments = $"/SCAN=\"{filePath}\" /REPORT=\"{filePathReport}\"";
            myProcess.StartInfo.Arguments = $"\"{filePath}\" /p=1 /r=\"{filePathReport}\"";
            myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            myProcess.Start();
            myProcess.WaitForExit();
            myProcess.Dispose();

            //add some time for report to be written to file
            for (var i = 0; i < 100; i++)
            {
                if (System.IO.File.Exists(filePathReport))
                    break;
                else
                    Thread.Sleep(100);
            }

            using (var streamReader = new StreamReader(filePathReport))
            {
                var fileContents = streamReader.ReadToEnd();
                isClean = fileContents.Contains("Infected files: 0");

                if (!isClean)
                    System.IO.File.Delete(filePath);
            }

            System.IO.File.Delete(filePathReport);

            return isClean;
        }
    }
}