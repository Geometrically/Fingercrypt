using System;
using System.IO;
using System.Linq;
using Microsoft.Research.SEAL;
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
            
            Console.WriteLine(BitConverter.ToString(HashFingerprint(lines)).Replace("-", "").Length);
            
            Cv2.WaitKey(0);
        }

        public static LineSegmentPoint[] GetImageLines(Mat img)
        {
            var skeleton = new Mat(img.Size(), MatType.CV_8UC1, new Scalar(0));
            
            Cv2.Canny(img, skeleton, 255/3, 255);
            Cv2.ImShow("Skeleton", skeleton);

            return Cv2.HoughLinesP(skeleton, 1, Cv2.PI / 180, 15);
        }
        
        public static byte[] HashFingerprintBatch(LineSegmentPoint[] lines)
        {
            var encryptionParams = new EncryptionParameters(SchemeType.BFV);

            ulong polyModulusDegree = 1024;
            
            encryptionParams.PolyModulusDegree = polyModulusDegree;
            encryptionParams.CoeffModulus = CoeffModulus.BFVDefault(polyModulusDegree);
            encryptionParams.PlainModulus = PlainModulus.Batching(polyModulusDegree, 20);

            var context = new SEALContext(encryptionParams);
            var keygen = new KeyGenerator(context);

            var encryptor = new Encryptor(context, keygen.PublicKey);
            
            var encoder = new BatchEncoder(context);

            var stream = new MemoryStream();
            
            var evaluator = new Evaluator(context);

            foreach (var lineChunk in lines.Split(Convert.ToInt32(polyModulusDegree)/2))
            {
                var slotCount = encoder.SlotCount;
                var podMatrix = new ulong[slotCount];

                var currentIndex = 0;
                
                foreach (var line in lineChunk)
                {
                    podMatrix[currentIndex] = Convert.ToUInt64(line.P1.X);
                    podMatrix[currentIndex + 1] = Convert.ToUInt64(line.P1.Y);
                    podMatrix[currentIndex + 2] = Convert.ToUInt64(line.P2.X);
                    podMatrix[currentIndex + 3] = Convert.ToUInt64(line.P2.Y);
                }
                var plainText = new Plaintext();
                
                encoder.Encode(podMatrix, plainText);

                var cipherText = new Ciphertext();

                encryptor.Encrypt(plainText, cipherText);
                
                cipherText.Save(stream);
                
                Console.WriteLine(BitConverter.ToString(stream.ToArray()).Replace("-", ""));
                break;
            }

            return stream.ToArray();
        }
        
        

        /*public static byte[] HashFingerprint(LineSegmentPoint[] lines)
        {
            var encryptionParams = new EncryptionParameters(SchemeType.CKKS);

            ulong polyModulusDegree = 8192;
            
            encryptionParams.PolyModulusDegree = polyModulusDegree;
            encryptionParams.CoeffModulus = CoeffModulus.Create(polyModulusDegree, new []{ 60, 40, 40, 60 });
            
            var context = new SEALContext(encryptionParams);
            var keygen = new KeyGenerator(context);
            
            var stream = new MemoryStream();

            var encryptor = new Encryptor(context, keygen.PublicKey);
            var encoder = new CKKSEncoder(context);
            
            ulong slotCount = encoder.SlotCount;
            
            Console.WriteLine(lines.Length);
            
            foreach (var line in lines)
            {
                Console.WriteLine(Array.IndexOf(lines, line));
                
                encoder.Encode();
                var p1X = new Ciphertext();
                var p1Y = new Ciphertext();
                var p2X = new Ciphertext();
                var p2Y = new Ciphertext();

                encryptor.Encrypt(new Plaintext(1.ToString()), p1X);
                encryptor.Encrypt(new Plaintext(line.P1.Y.ToString()), p1Y);
                encryptor.Encrypt(new Plaintext(line.P2.X.ToString()), p2X);
                encryptor.Encrypt(new Plaintext(line.P2.Y.ToString()), p2Y);
                
                
                p1X.Save(stream);
                p1Y.Save(stream);
                p2X.Save(stream);
                p2Y.Save(stream); 
            }

            
            Console.WriteLine(BitConverter.ToString(stream.ToArray()).Replace("-", "").Length);

            return fingerprintHash.ToArray();
        }*/
        
    }
}
