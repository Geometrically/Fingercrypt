using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenCvSharp;

namespace Fingercrypt
{
    class Program
    {
        public static void Main(string[] args)
        {

            var img = Cv2.ImRead("image.jpg", 0);
            Cv2.ImShow("Original", img);
            
            Cv2.BitwiseNot(img, img);
            Cv2.Threshold(img, img, 127, 255, ThresholdTypes.Binary);

            var lines = GetImageLines(img);

            CheckFingerprint(BitConverter.ToString(HashFingerprint(lines)).Replace("-",""), lines);

            Cv2.WaitKey(0);
            
            
        }

        private static LineSegmentPoint[] GetImageLines(Mat img)
        {
            var skeleton = new Mat(img.Size(), MatType.CV_8UC1, new Scalar(0));
            
            Cv2.Canny(img, skeleton, 255/3, 255);
            Cv2.ImShow("Skeleton", skeleton);

            return Cv2.HoughLinesP(skeleton, 1, Cv2.PI / 180, 15);
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

        public static bool CheckFingerprint(string hashedLines, LineSegmentPoint[] lines, int chunkLength=64, int linesPerChunk=1, int allowedVariation=1)
        {
            using (var sha512 = new SHA512Managed())
            {
                var currentChunk = 0;
                var chunks = lines.Split(linesPerChunk).ToArray();
                
                foreach (var hashedLineChunk in hashedLines.ToCharArray().Split(chunkLength))
                {
                    foreach (var possibleOffset in Enumerable.Range(0, allowedVariation + 1).Combinations(linesPerChunk*4))
                    {
                        Console.WriteLine(chunks.Length);
                        //Positive Side
                        var currentOffsetValue = 0;
                        
                        var plainChunk = new StringBuilder();
                        Console.WriteLine(currentChunk);
                        foreach (var line in chunks[currentChunk])
                        {
                            plainChunk.Append(line.P1.X + Convert.ToInt32(possibleOffset[currentOffsetValue]));
                            plainChunk.Append(line.P1.Y + Convert.ToInt32(possibleOffset[currentOffsetValue + 1]));
                            plainChunk.Append(line.P2.X + Convert.ToInt32(possibleOffset[currentOffsetValue + 2]));
                            plainChunk.Append(line.P2.Y + Convert.ToInt32(possibleOffset[currentOffsetValue + 3]));
                            
                            currentOffsetValue += 4;
                        }
                        
                        var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(plainChunk.ToString()));
                        
                        if (BitConverter.ToString(hash).Replace("-","") == new string(hashedLineChunk.ToArray()))
                            return true;
                        
                        //Negative Side
                        currentOffsetValue = 0;
                        
                        plainChunk = new StringBuilder();
                        
                        foreach (var line in chunks[currentChunk])
                        {
                            plainChunk.Append(line.P1.X - Convert.ToInt32(possibleOffset[currentOffsetValue]));
                            plainChunk.Append(line.P1.Y - Convert.ToInt32(possibleOffset[currentOffsetValue + 1]));
                            plainChunk.Append(line.P2.X - Convert.ToInt32(possibleOffset[currentOffsetValue + 2]));
                            plainChunk.Append(line.P2.Y - Convert.ToInt32(possibleOffset[currentOffsetValue + 3]));

                            currentOffsetValue += 4;
                        }

                        hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(plainChunk.ToString()));
                        
                        if (BitConverter.ToString(hash).Replace("-","") == new string(hashedLineChunk.ToArray()))
                            return true;
                    }
                    currentChunk++;
                }
            }
            return false;
        }
    }
}
