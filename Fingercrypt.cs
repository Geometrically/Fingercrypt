using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenCvSharp;

namespace Fingercrypt
{
    public class Fingercrypt
    {
        public static string HashFingerprint(Mat img, bool inverseColors)
        {
            if (inverseColors)
                Cv2.BitwiseNot(img, img);
            
            Cv2.Threshold(img, img, 127, 255, ThresholdTypes.Binary);

            var lines = GetImageLines(img);

            return BitConverter.ToString(HashFingerprint(lines)).Replace("-", "");
        }
        
        public static byte[] HashFingerprint(LineSegmentPoint[] lines, int linesPerChunk=1, int chunkSize=16)
        {
            var stream = new MemoryStream();

            using (var sha512 = new SHA512Managed())
            {
                foreach (var lineChunk in lines.Split(linesPerChunk))
                {
                    var plainChunk = new StringBuilder();

                    foreach (var line in lineChunk)
                    {
                        plainChunk.Append(line.P1.X);
                        plainChunk.Append(line.P1.Y);
                        plainChunk.Append(line.P2.X);
                        plainChunk.Append(line.P2.Y);
                    }

                    var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(plainChunk.ToString()));
                    
                    stream.Write(hash);
                }
            }
            
            return stream.ToArray();
        }

        public static bool CheckFingerprint(string hashedLines, LineSegmentPoint[] lines, float chunkPercentThresold,
            int chunkLength = 128, int linesPerChunk = 1, int allowedVariation = 1)
        {
            var chunks = lines.Split(linesPerChunk).ToArray();

            var matchingChunks = 0;
            var numberOfChunks = chunks.Length;

            using (var sha512 = new SHA512Managed())
            {
                var currentChunk = 0;

                foreach (var hashedLineChunk in hashedLines.SplitByLength(chunkLength))
                {
                    foreach (var possibleOffset in Enumerable.Range(0, allowedVariation + 1)
                        .Combinations(linesPerChunk * 4))
                    {
                        if (currentChunk >= numberOfChunks) break;

                        //Positive Side
                        var currentOffsetValue = 0;

                        var plainChunk = new StringBuilder();
                        foreach (var line in chunks[currentChunk])
                        {
                            plainChunk.Append(line.P1.X + char.GetNumericValue(possibleOffset[currentOffsetValue]));
                            plainChunk.Append(line.P1.Y + char.GetNumericValue(possibleOffset[currentOffsetValue + 1]));
                            plainChunk.Append(line.P2.X + char.GetNumericValue(possibleOffset[currentOffsetValue + 2]));
                            plainChunk.Append(line.P2.Y + char.GetNumericValue(possibleOffset[currentOffsetValue + 3]));

                            currentOffsetValue += 4;
                        }

                        var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(plainChunk.ToString()));

                        if (BitConverter.ToString(hash).Replace("-", "") == hashedLineChunk)
                        {
                            matchingChunks += 1;
                            break;
                        }

                        //Negative Side
                        currentOffsetValue = 0;

                        plainChunk = new StringBuilder();

                        foreach (var line in chunks[currentChunk])
                        {
                            plainChunk.Append(line.P1.X + char.GetNumericValue(possibleOffset[currentOffsetValue]));
                            plainChunk.Append(line.P1.Y + char.GetNumericValue(possibleOffset[currentOffsetValue + 1]));
                            plainChunk.Append(line.P2.X + char.GetNumericValue(possibleOffset[currentOffsetValue + 2]));
                            plainChunk.Append(line.P2.Y + char.GetNumericValue(possibleOffset[currentOffsetValue + 3]));

                            currentOffsetValue += 4;
                        }

                        hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(plainChunk.ToString()));

                        if (BitConverter.ToString(hash).Replace("-", "") == hashedLineChunk)
                        {
                            matchingChunks += 1;
                            break;
                        }
                    }

                    currentChunk++;
                }
            }
            
            return matchingChunks / numberOfChunks >= chunkPercentThresold/100;
        }
        
        public static LineSegmentPoint[] GetImageLines(Mat img)
        {
            var skeleton = new Mat(img.Size(), MatType.CV_8UC1, new Scalar(0));
            
            Cv2.Canny(img, skeleton, 255/3, 255);

            var unCroppedLines = Cv2.HoughLinesP(skeleton, 1, Cv2.PI / 180, 15, 0);

            var minX = int.MaxValue;
            var maxX = int.MinValue;
            
            var minY = int.MaxValue;
            var maxY = int.MinValue;

            foreach (var lines in unCroppedLines)
            {
                if (lines.P1.X < minX)
                    minX = lines.P1.X;
                
                if (lines.P2.X < minX)
                    minX = lines.P2.X;
                
                if (lines.P1.X > maxX)
                    maxX = lines.P1.X;
                
                if (lines.P2.X > maxX)
                    maxX = lines.P2.X;
                
                if (lines.P1.Y < minY)
                    minY = lines.P1.Y;
                
                if (lines.P2.Y < minY)
                    minY = lines.P2.Y;
                
                if (lines.P1.Y > maxY)
                    maxY = lines.P1.Y;
                
                if (lines.P2.Y > maxY)
                    maxY = lines.P2.Y;
            }

            var croppedImage = new Mat(skeleton, Rect.FromLTRB(minX, minY, maxX, maxY));

            return Cv2.HoughLinesP(croppedImage, 1, Cv2.PI / 180, 15, 10, 5);
        }
    }
}
