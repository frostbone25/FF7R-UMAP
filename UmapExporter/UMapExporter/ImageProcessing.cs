using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//NOTE: SkiaSharp comes from CUE4Parse
using SkiaSharp;

namespace UMapExporter
{
    public static class ImageProcessing
    {
        public static void CombineAlbedoWithAlpha(string originalAlbedoPath, string originalAlphaPath)
        {
            string newAlbedoBitmapPath = string.Format("{0}/{1}_ALBEDO{2}", Path.GetDirectoryName(originalAlbedoPath), Path.GetFileNameWithoutExtension(originalAlbedoPath), Path.GetExtension(originalAlbedoPath));

            SKImage originalAlbedoImage = SKImage.FromEncodedData(originalAlbedoPath);
            SKBitmap originalAlbedoBitmap = SKBitmap.FromImage(originalAlbedoImage);

            SKImage originalAlphaImage = SKImage.FromEncodedData(originalAlphaPath);
            SKBitmap originalAlphaBitmap = SKBitmap.FromImage(originalAlphaImage);

            SKBitmap newAlbedoBitmap = new SKBitmap(originalAlbedoBitmap.Width, originalAlbedoBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

            for (int x = 0; x < originalAlbedoBitmap.Width; x++)
            {
                for (int y = 0; y < originalAlbedoBitmap.Height; y++)
                {
                    SKColor originalAlbedoColor = originalAlbedoBitmap.GetPixel(x, y);
                    SKColor originalAlphaColor = originalAlphaBitmap.GetPixel(x, y);

                    SKColor newAlbedoColor = new SKColor(originalAlbedoColor.Red, originalAlbedoColor.Green, originalAlbedoColor.Blue, originalAlphaColor.Red);
                    newAlbedoBitmap.SetPixel(x, y, newAlbedoColor);
                }
            }

            using (FileStream newFileStream = File.Create(newAlbedoBitmapPath))
            {
                if(newAlbedoBitmap.Encode(newFileStream, SKEncodedImageFormat.Png, 100))
                    ConsoleWriter.WriteSuccessLine(string.Format("Combined Albedo and Alpha! {0}", newAlbedoBitmapPath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Failed to combine Albedo and Alpha! {0}", newAlbedoBitmapPath));
            }

            originalAlbedoImage.Dispose();
            originalAlbedoBitmap.Dispose();
            originalAlphaImage.Dispose();
            originalAlphaBitmap.Dispose();
            newAlbedoBitmap.Dispose();
        }

        public static void ExtractPBRMaps(string originalPBRPath)
        {
            string newMetallicPath = string.Format("{0}/{1}_METALLIC{2}", Path.GetDirectoryName(originalPBRPath), Path.GetFileNameWithoutExtension(originalPBRPath), Path.GetExtension(originalPBRPath));
            string newRoughnessPath = string.Format("{0}/{1}_ROUGHNESS{2}", Path.GetDirectoryName(originalPBRPath), Path.GetFileNameWithoutExtension(originalPBRPath), Path.GetExtension(originalPBRPath));
            string newVariantPath = string.Format("{0}/{1}_VARIANT{2}", Path.GetDirectoryName(originalPBRPath), Path.GetFileNameWithoutExtension(originalPBRPath), Path.GetExtension(originalPBRPath));

            SKImage originalPBRImage = SKImage.FromEncodedData(originalPBRPath);
            SKBitmap originalPBRBitmap = SKBitmap.FromImage(originalPBRImage);

            SKBitmap newMetallicBitmap = new SKBitmap(originalPBRBitmap.Width, originalPBRBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            SKBitmap newRoughnessBitmap = new SKBitmap(originalPBRBitmap.Width, originalPBRBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            SKBitmap newVariantBitmap = new SKBitmap(originalPBRBitmap.Width, originalPBRBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);

            for (int x = 0; x < originalPBRBitmap.Width; x++)
            {
                for (int y = 0; y < originalPBRBitmap.Height; y++)
                {
                    SKColor originalPBRColor = originalPBRBitmap.GetPixel(x, y);

                    SKColor newMetallicColor = new SKColor(originalPBRColor.Red, originalPBRColor.Red, originalPBRColor.Red, byte.MaxValue);
                    SKColor newRoughnessColor = new SKColor(originalPBRColor.Green, originalPBRColor.Green, originalPBRColor.Green, byte.MaxValue);
                    SKColor newVariantColor = new SKColor(originalPBRColor.Blue, originalPBRColor.Blue, originalPBRColor.Blue, byte.MaxValue);

                    newMetallicBitmap.SetPixel(x, y, newMetallicColor);
                    newRoughnessBitmap.SetPixel(x, y, newRoughnessColor);
                    newVariantBitmap.SetPixel(x, y, newVariantColor);
                }
            }

            using (FileStream newFileStream = File.Create(newMetallicPath))
            {
                if (newMetallicBitmap.Encode(newFileStream, SKEncodedImageFormat.Png, 100))
                    ConsoleWriter.WriteSuccessLine(string.Format("Seperated Metallic from PBR! {0}", newMetallicPath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Failed to seperate metallic from PBR! {0}", newMetallicPath));
            }

            using (FileStream newFileStream = File.Create(newRoughnessPath))
            {
                if (newRoughnessBitmap.Encode(newFileStream, SKEncodedImageFormat.Png, 100))
                    ConsoleWriter.WriteSuccessLine(string.Format("Seperated Roughness from PBR! {0}", newRoughnessPath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Failed to seperate roughness from PBR! {0}", newRoughnessPath));
            }

            using (FileStream newFileStream = File.Create(newVariantPath))
            {
                if (newVariantBitmap.Encode(newFileStream, SKEncodedImageFormat.Png, 100))
                    ConsoleWriter.WriteSuccessLine(string.Format("Seperated Variant from PBR! {0}", newVariantPath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Failed to seperate variant from PBR! {0}", newVariantPath));
            }

            originalPBRImage.Dispose();
            originalPBRBitmap.Dispose();
            newMetallicBitmap.Dispose();
            newRoughnessBitmap.Dispose();
            newVariantBitmap.Dispose();
        }

        public static void RemixPBR_ForUnityHDRP(string originalPBRPath, string originalOcclusionPath = "")
        {
            string newPBRPath = string.Format("{0}/{1}_UNITYHDRP{2}", Path.GetDirectoryName(originalPBRPath), Path.GetFileNameWithoutExtension(originalPBRPath), Path.GetExtension(originalPBRPath));

            SKImage originalPBRImage = SKImage.FromEncodedData(originalPBRPath);
            SKBitmap originalPBRBitmap = SKBitmap.FromImage(originalPBRImage);

            SKImage originalOcclusionImage = null;
            SKBitmap originalOcclusionBitmap = null;

            if (string.IsNullOrEmpty(originalPBRPath) == false)
            {
                if(File.Exists(originalOcclusionPath))
                {
                    originalOcclusionImage = SKImage.FromEncodedData(originalPBRPath);
                    originalOcclusionBitmap = SKBitmap.FromImage(originalPBRImage);
                }
            }

            SKBitmap newPBRBitmap = new SKBitmap(originalPBRBitmap.Width, originalPBRBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

            for (int x = 0; x < originalPBRBitmap.Width; x++)
            {
                for (int y = 0; y < originalPBRBitmap.Height; y++)
                {
                    SKColor originalPBRColor = originalPBRBitmap.GetPixel(x, y);

                    byte finalOcclusionValue = byte.MaxValue;

                    if(originalOcclusionBitmap != null)
                        finalOcclusionValue = originalOcclusionBitmap.GetPixel(x, y).Red;

                    float roughness = (float)originalPBRColor.Green / (float)byte.MaxValue;
                    roughness = 1.0f - (float)Math.Sqrt(roughness);

                    byte finalRoughnessValue = (byte)(roughness * byte.MaxValue);

                    SKColor newPBRColor = new SKColor(originalPBRColor.Red, finalOcclusionValue, 0, finalRoughnessValue);

                    newPBRBitmap.SetPixel(x, y, newPBRColor);
                }
            }

            using (FileStream newFileStream = File.Create(newPBRPath))
            {
                if (newPBRBitmap.Encode(newFileStream, SKEncodedImageFormat.Png, 100))
                    ConsoleWriter.WriteSuccessLine(string.Format("Created UNITY HDRP Texture Map! {0}", newPBRPath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Failed to create UNITY HDRP Texture Map! {0}", newPBRPath));
            }

            originalPBRImage.Dispose();
            originalPBRBitmap.Dispose();

            if(originalOcclusionImage != null)
                originalOcclusionImage.Dispose();

            if (originalOcclusionBitmap != null)
                originalOcclusionBitmap.Dispose();

            newPBRBitmap.Dispose();
        }

        public static void RemixPBR_ForUnityURP(string originalPBRPath, string originalOcclusionPath = "")
        {
            string newPBRPath = string.Format("{0}/{1}_UNITYURP{2}", Path.GetDirectoryName(originalPBRPath), Path.GetFileNameWithoutExtension(originalPBRPath), Path.GetExtension(originalPBRPath));

            SKImage originalPBRImage = SKImage.FromEncodedData(originalPBRPath);
            SKBitmap originalPBRBitmap = SKBitmap.FromImage(originalPBRImage);

            SKImage originalOcclusionImage = null;
            SKBitmap originalOcclusionBitmap = null;

            if (string.IsNullOrEmpty(originalPBRPath) == false)
            {
                if (File.Exists(originalOcclusionPath))
                {
                    originalOcclusionImage = SKImage.FromEncodedData(originalPBRPath);
                    originalOcclusionBitmap = SKBitmap.FromImage(originalPBRImage);
                }
            }

            SKBitmap newPBRBitmap = new SKBitmap(originalPBRBitmap.Width, originalPBRBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

            for (int x = 0; x < originalPBRBitmap.Width; x++)
            {
                for (int y = 0; y < originalPBRBitmap.Height; y++)
                {
                    SKColor originalPBRColor = originalPBRBitmap.GetPixel(x, y);

                    byte finalOcclusionValue = byte.MaxValue;

                    if (originalOcclusionBitmap != null)
                        finalOcclusionValue = originalOcclusionBitmap.GetPixel(x, y).Red;

                    float roughness = (float)originalPBRColor.Green / (float)byte.MaxValue;
                    roughness = 1.0f - (float)Math.Sqrt(roughness); //convert to unity smoothness

                    byte finalSmoothnessValue = (byte)(roughness * byte.MaxValue);

                    SKColor newPBRColor = new SKColor(originalPBRColor.Red, finalOcclusionValue, 0, finalSmoothnessValue);

                    newPBRBitmap.SetPixel(x, y, newPBRColor);
                }
            }

            using (FileStream newFileStream = File.Create(newPBRPath))
            {
                if (newPBRBitmap.Encode(newFileStream, SKEncodedImageFormat.Png, 100))
                    ConsoleWriter.WriteSuccessLine(string.Format("Created UNITY HDRP Texture Map! {0}", newPBRPath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Failed to create UNITY HDRP Texture Map! {0}", newPBRPath));
            }
        }

        public static void RemixPBR_ForUnityBIRP(string originalPBRPath)
        {
            string newPBRPath = string.Format("{0}/{1}_UNITYBIRP{2}", Path.GetDirectoryName(originalPBRPath), Path.GetFileNameWithoutExtension(originalPBRPath), Path.GetExtension(originalPBRPath));

            SKImage originalPBRImage = SKImage.FromEncodedData(originalPBRPath);
            SKBitmap originalPBRBitmap = SKBitmap.FromImage(originalPBRImage);

            SKBitmap newPBRBitmap = new SKBitmap(originalPBRBitmap.Width, originalPBRBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

            for (int x = 0; x < originalPBRBitmap.Width; x++)
            {
                for (int y = 0; y < originalPBRBitmap.Height; y++)
                {
                    SKColor originalPBRColor = originalPBRBitmap.GetPixel(x, y);

                    float roughness = (float)originalPBRColor.Green / (float)byte.MaxValue;
                    roughness = 1.0f - (float)Math.Sqrt(roughness); //convert to unity smoothness

                    byte finalSmoothnessValue = (byte)(roughness * byte.MaxValue);

                    SKColor newPBRColor = new SKColor(originalPBRColor.Red, originalPBRColor.Red, originalPBRColor.Red, finalSmoothnessValue);

                    newPBRBitmap.SetPixel(x, y, newPBRColor);
                }
            }

            using (FileStream newFileStream = File.Create(newPBRPath))
            {
                if (newPBRBitmap.Encode(newFileStream, SKEncodedImageFormat.Png, 100))
                    ConsoleWriter.WriteSuccessLine(string.Format("Created UNITY HDRP Texture Map! {0}", newPBRPath));
                else
                    ConsoleWriter.WriteErrorLine(string.Format("Failed to create UNITY HDRP Texture Map! {0}", newPBRPath));
            }
        }
    }
}
