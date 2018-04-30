using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace Components.Helpers
{
    public static class ImageUpload
    {
        #region Configuration
        //Tam boyutlu resimlerin yolu
        public const string PATH = "/Content/images/Upload/";
        //Thumbnails yolu
        public const string PATHt = "/Content/images/Thumbnails/";
        public const string WebSite = "http://www.sample.com";
        #endregion

        public static string MapPath { get => HttpContext.Current.Server.MapPath(PATH); }
        public static string MapPatht { get => HttpContext.Current.Server.MapPath(PATHt); }

        public enum CropPosition
        {
            None,
            Center,
            Left,
            Right
        }

        private static void CheckFilePaths()
        {

            var pathsT = PATHt.Split('/');
            var pathSumT = "/";
            foreach (var item in pathsT)
            {
                var pathCombined = HttpContext.Current.Server.MapPath(pathSumT);
                if (!Directory.Exists(pathCombined))
                {
                    Directory.CreateDirectory(pathCombined);
                }
                pathSumT += item + "/";
            }

            var paths = PATH.Split('/');
            var pathSum = "/";
            foreach (var item in paths)
            {
                var pathCombined = HttpContext.Current.Server.MapPath(pathSum);
                if (!Directory.Exists(pathCombined))
                {
                    Directory.CreateDirectory(pathCombined);
                }
                pathSum += item + "/";
            }
        }

        private static Bitmap ImageResizeByWidth(System.Drawing.Image _image, int width)
        {
            float ratio = _image.Height / (float)_image.Width;
            return new Bitmap(_image, width, Convert.ToInt32((width * ratio)));
            //ÖNEMLİ !!!
            //KULLANILDIKTAN SONRA RETURN EDİLMİŞ BİTMAP DISPOSE EDİLMELİ !!!

        }
        private static Bitmap ImageResizeByWidth(Bitmap _bmp, int width)
        {
            float ratio = _bmp.Height / (float)_bmp.Width;
            return new Bitmap(_bmp, width, Convert.ToInt32((width * ratio)));
        }
        private static Bitmap ImageResizeByHeight(System.Drawing.Image _image, int height)
        {
            Bitmap originalImage = new Bitmap(_image);
            float ratio = _image.Height / (float)_image.Width;

            return new Bitmap(originalImage, (int)(height / ratio), height);
        }
        private static Bitmap ImageResizeByHeight(Bitmap _bmp, int height)
        {
            Bitmap originalImage = new Bitmap(_bmp);
            float ratio = _bmp.Height / (float)_bmp.Width;

            return new Bitmap(originalImage, (int)(height / ratio), height);
        }
        /// <summary>
        /// !!! Resim bozulabilir !!! Resim süneiblir veya sıkışabilir! \n
        /// Oran bilinmiyorsa ImageResizeAndCrop kullanılmalı!
        /// </summary>
        /// <returns></returns>
        private static Bitmap ImageResizeByCustom(Bitmap _bmp, int width, int height)
        {
            return _bmp.Clone(new Rectangle(new Point(0, 0), new Size(width, height)), _bmp.PixelFormat);
        }

        /// <summary>
        /// Resmi istenilen boyuta getirir ve oranı bozulmaması (sünme, sıkıştırma) için kırpar.
        /// </summary>
        /// <param name="_image">Kırpılacak resim Image tipinde</param>
        /// <param name="width">Kırpılacak Genişlik</param>
        /// <param name="height">Kırpılacak Yükseklik</param>
        /// <param name="_cPos">Kırpma işleminin merkez odağı</param>
        /// <returns>Return edilen Bitmap DISPOSE edilmeli!!!</returns>
        private static Bitmap ImageResizeAndCrop(Bitmap _image, int width, int height, CropPosition _cPos = CropPosition.Center)
        {
            Rectangle cropRect = new Rectangle(0, 0, height, width);

            Bitmap _sourceBmp;

            //Optimised for ratio
            // Compared image ratio and requested ratio
            if ((_image.Height / (float)_image.Width) < (height / (float)width))
                _sourceBmp = ImageResizeByHeight(_image, height);
            else
                _sourceBmp = ImageResizeByWidth(_image, width);


            switch (_cPos)
            {
                case CropPosition.Center:
                    cropRect = new Rectangle(new Point((int)((_sourceBmp.Width / 2f) - (width / 2f)), (int)((_sourceBmp.Height / 2f) - (height / 2f))), new Size(width.Clamp(0, _sourceBmp.Width), height.Clamp(0, _sourceBmp.Height)));
                    break;
                case CropPosition.Left:
                    cropRect = new Rectangle(new Point(0, (_sourceBmp.Height / 2) - (height / 2)), new Size(width.Clamp(0, _sourceBmp.Width), height.Clamp(0, _sourceBmp.Height)));
                    break;
                case CropPosition.Right:
                    cropRect = new Rectangle(new Point(_sourceBmp.Height - width, (_sourceBmp.Height / 2) - (height / 2)), new Size(width.Clamp(0, _sourceBmp.Width), height.Clamp(0, _sourceBmp.Height)));
                    break;
            }
            Bitmap _target = new Bitmap(cropRect.Width, cropRect.Height);

            using (Graphics g = Graphics.FromImage(_target))
            {
                g.DrawImage(_sourceBmp, new Rectangle(0, 0, _target.Width, _target.Height), cropRect, GraphicsUnit.Pixel);
                return _target;
            }

        }

        private static void SaveBitmap(Bitmap bmp, string savingPath)
        {
            CheckFilePaths();
            using (FileStream stream = File.Create(savingPath))
            {
                System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
                myEncoderParameter = new EncoderParameter(myEncoder, 100L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                bmp.Save(stream, GetEncoder(ImageFormat.Png), myEncoderParameters);
                bmp.Dispose();
            }
        }

        #region Methods will be used

        public static Tuple<string, string> SaveImageWithThumbnail(HttpPostedFileBase file, int width, int height, int widthT, int heightT, bool pathWithWebsite = false)
        {
            CheckFilePaths();
            using (var bmp = new Bitmap(file.InputStream))
            {
                string fileName = Guid.NewGuid().ToString().Replace("-", String.Empty) + ".png";

                SaveBitmap(ImageResizeAndCrop(bmp, width, height), Path.Combine(MapPath, fileName));
                SaveBitmap(ImageResizeAndCrop(bmp, widthT, heightT), Path.Combine(MapPatht, fileName));

                return new Tuple<string, string>((pathWithWebsite ? WebSite : "") + PATH + fileName, (pathWithWebsite ? WebSite : "") + PATHt + fileName);

            }
        }

        public static string SaveImage(HttpPostedFileBase file, int width, int height, bool pathWithWebsite = false)
        {
            CheckFilePaths();
            using (var bmp = new Bitmap(file.InputStream))
            {

                string fileName = Guid.NewGuid().ToString().Replace("-", String.Empty) + ".png";
                SaveBitmap(ImageResizeAndCrop(bmp, width, height), Path.Combine(MapPath, fileName));

                if (pathWithWebsite) return WebSite + PATH + fileName;
                return PATH + fileName;
            }
        }

        internal static bool DeleteByPath(string Path)
        {
            FileInfo file = new FileInfo(HttpContext.Current.Server.MapPath("~" + Path));
            if (file.Exists)
            {
                file.Delete();
                //System.IO.File.Delete("~" + Path);
                return true;
            }
            return false;
        }

        #endregion

        #region HelperMethods
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        internal static System.Drawing.Image Base64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                System.Drawing.Image image = System.Drawing.Image.FromStream(ms, true);
                return image;
            }

        }

        internal static string SaveBase64Image(string base64string, string customImgName = null)
        {
            var _image = Base64ToImage(base64string);

            if (_image == null) return null;
            string imgName = System.Guid.NewGuid().ToString().Replace("-", String.Empty) + customImgName;
            string savingPath = HttpContext.Current.Server.MapPath(PATH + imgName + ".png");
            using (var bmp = new Bitmap(_image))
            {
                SaveBitmap(bmp, savingPath);
            }

            return WebSite + PATH + imgName;
        }
        #endregion        
    }

    public static class ExtensionsHelper
    {
        public static T Clamp<T>(this T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            else if (value.CompareTo(max) > 0) return max;
            else return value;
        }


    }
}