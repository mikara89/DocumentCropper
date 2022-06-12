using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentCropper.Lib
{
    public class TransformImageProcessor : IDisposable, IImageProcessor
    {
        List<Point2f> point2Fs;
        bool IsCuted;
        Point2f[] srcPoints;

        public TransformImageProcessor()
        {
            point2Fs = new List<Point2f>();
            IsCuted = false;
            srcPoints = new Point2f[] {
                new Point2f(0, 0),
                new Point2f(0, 0),
                new Point2f(0, 0),
                new Point2f(0, 0),
            };
        }

        public byte[] Process(Stream image, ProcessResultType resultType, string ext = ".png")
        {
            var transformedImage = execute(Mat.FromStream(image, ImreadModes.Unchanged));

            switch (resultType)
            {
                case ProcessResultType.PDF:
                    return ImagesToPdf(transformedImage);

                case ProcessResultType.IMG:
                    return transformedImage.ToMemoryStream(ext).GetBuffer();

                default:
                    throw new NotImplementedException();
            }
        }

        public Task<byte[]> ProcessAsync(Stream image, ProcessResultType resultType, string ext = ".png")
        {
            return Task.Run(() =>
            {
                return Process(image, resultType, ext);
            });
        }

        private Mat execute(Mat OriginalImage)
        {

            //clone image
            Mat modifiedImage = new Mat(OriginalImage.Rows, OriginalImage.Cols, OriginalImage.Type());
            OriginalImage.CopyTo(modifiedImage);


            //Step 1 Grayscale
            modifiedImage = modifiedImage.CvtColor(ColorConversionCodes.BGR2GRAY);


            //Step 2 Blur the image
            //modifiedImage = modifiedImage.GaussianBlur(new Size(5, 5), 0);
            modifiedImage = modifiedImage.MedianBlur(3);


            //Step 3 find edges (Canny and Dilate)
            modifiedImage = modifiedImage.Canny(50, 200);

            // dilate canny output to remove potential
            // holes between edge segments
            modifiedImage = modifiedImage.Dilate(null);


            //Step 4 Find Contour with 4 points (rectangle) with lagest area (find the doc edges)

            HierarchyIndex[] hierarchyIndexes;
            Point[][] contours;
            modifiedImage.FindContours(out contours, out hierarchyIndexes, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            //find largest area with 4 points
            double largestarea = 0;
            var largestareacontourindex = 0;
            var contourIndex = 0;
            Point[] docEdgesPoints = null;

            //debug purpose, uncomment to see all contours captured by openCV
            //var contourImage = debug_showallcontours(OriginalImage, hierarchyIndexes, contours);



            foreach (var cont in contours)
            {
                var peri = Cv2.ArcLength(cont, true); //only take contour area that are closed shape no gap 
                var approx = Cv2.ApproxPolyDP(cont, 0.02 * peri, true);

                //TODO: we need to check and to not tranform if the contour size is larger or = to the picture size, 
                //or smaller than certain size means lagest contour detected is incorrect. then we output original image without transform
                if (approx.Length == 4 && Cv2.ContourArea(contours[contourIndex]) > largestarea)
                {
                    largestarea = Cv2.ContourArea(contours[contourIndex]);
                    largestareacontourindex = contourIndex;
                    docEdgesPoints = approx;
                }

                contourIndex = hierarchyIndexes[contourIndex].Next;
            }

            //Steps 4.1 find the max size of contour area (entire image) 
            //to be used to check if the largest contour area is the doc edges (ratio)
            var imageSize = OriginalImage.Size().Height * OriginalImage.Size().Width;

            // Steps 5: apply the four point transform to obtain a top-down
            // view of the original image
            Mat transformImage = null;
            var size = Cv2.ContourArea(contours[largestareacontourindex]);
            if (size < imageSize * 0.3) /*(false)*/
            {
                //if largest contour smaller than 50% of the picture, assume document edges not found
                //proceed with simple filter 

                transformImage = apply_doc_filters(OriginalImage);

            }
            else
            {
                IsCuted = true;
                //doc closed edges detected, proceed tranformation

                //convert to point2f
                foreach (var item in docEdgesPoints)
                {
                    point2Fs.Add(new Point2f(item.X, item.Y));
                }
                transformImage = transform(OriginalImage, point2Fs);
                if (transformImage != null)
                {

                    //Step 6: grayscale it to give it that 'black and white' paper effect
                    transformImage = apply_doc_filters(transformImage);
                }

            }

            if (transformImage != null)
            {
                return transformImage;
            }

            throw new InvalidOperationException("Image failed to transform.");

        }

        private Mat apply_doc_filters(Mat image)
        {
            //if closed rectangle of the document cant be detected then we will not transform the image but just apply simple filter to make it look like scanned doc

            //apply grayscale
            //Step 6: grayscale it to give it that 'black and white' paper effect
            image = image.CvtColor(ColorConversionCodes.BGR2GRAY);
            //transformImage = transformImage.Threshold(127, 255, ThresholdTypes.Binary);
            //transformImage = transformImage.Dilate(null);
            image = image.AdaptiveThreshold(255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 17, 11);

            ////add a border to the image to act as border of the doc
            //modifiedImage = modifiedImage.CopyMakeBorder(5, 5, 5, 5, BorderTypes.Constant, value: Scalar.Black);

            return image;
        }
        private Mat transform(Mat OriginalImage, List<Point2f> pts)
        {
            Mat dst = null;
            try
            {
                if (pts.Count == 4)
                {
                    //need to sort the points to follow order (bl, tl, tr, br), findcontours will return random order
                    var sortedpts = Sort(pts);

                    // calc new image height & width
                    // compute the width of the new image, which will be the
                    // maximum distance
                    var widthA = sortedpts[2].X - sortedpts[1].X;
                    var widthB = sortedpts[3].X - sortedpts[0].X;
                    var maxWidth = Math.Max((int)widthA, (int)widthB);

                    var heightA = sortedpts[1].Y - sortedpts[0].Y;
                    var heightB = sortedpts[2].Y - sortedpts[3].Y;
                    var maxHeight = Math.Max((int)heightA, (int)heightB);

                    srcPoints = sortedpts.ToArray();

                    //new output image size
                    //(tl, tr, br, bl)
                    Point2f[] dstPoints = new Point2f[] {
                    new Point2f(0, 0),
                    new Point2f(0, maxHeight - 1),
                    new Point2f(maxWidth - 1, maxHeight - 1),
                    new Point2f(maxWidth - 1, 0),
                };


                    var matrix = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);
                    dst = new Mat(new Size(maxWidth, maxHeight), MatType.CV_8UC3);
                    Cv2.WarpPerspective(OriginalImage, dst, matrix, dst.Size());
                    point2Fs.Clear();

                }
            }
            catch { }

            return dst;
        }

        private List<Point2f> Sort(List<Point2f> input)
        {
            List<Point2f> returnVal = new List<Point2f>();
            //sort into this order (bl, tl, tr, br)
            if (input.Count == 4)
            {
                //left, sort point by lowest X
                var left2 = input.OrderBy(p => p.X).Take(2);

                //Right, sort by highest X
                var right2 = input.OrderByDescending(p => p.X).Take(2);

                //bl
                returnVal.Add(left2.OrderBy(p => p.Y).First());
                //tl
                returnVal.Add(left2.OrderByDescending(p => p.Y).First());
                //tr
                returnVal.Add(right2.OrderByDescending(p => p.Y).First());
                //br
                returnVal.Add(right2.OrderBy(p => p.Y).First());
            }

            return returnVal;
        }

        private byte[] ImagesToPdf(Mat imageOrg)
        {
            using (var imageMemoryStream = imageOrg.ToMemoryStream())
            {
                iTextSharp.text.Rectangle pageSize = null;
                pageSize = new iTextSharp.text.Rectangle(0, 0, imageOrg.Size().Width, imageOrg.Size().Height);
                var ms = new MemoryStream();

                var document = new iTextSharp.text.Document(pageSize, 0, 0, 0, 0);
                iTextSharp.text.pdf.PdfWriter.GetInstance(document, ms).SetFullCompression();
                document.Open();
                var image = iTextSharp.text.Image.GetInstance(imageMemoryStream.ToArray(), true);
                document.Add(image);
                document.Close();

                return ms.ToArray();
            }
        }

        public void Dispose()
        {
            point2Fs = null;
            srcPoints = null;
            GC.Collect();
        }

        public enum ProcessResultType
        {
            PDF,
            IMG
        }
    }
}
