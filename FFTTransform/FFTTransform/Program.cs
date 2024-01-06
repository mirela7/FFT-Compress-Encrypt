using Emgu.CV;
using Emgu.CV.Structure;
using FFTTransform.Utils;
using System.IO;

namespace FFTTransform
{

    internal class Program
    {
        const string OPERATION_COMPRESS = "compress";
        const string OPERATION_OPEN = "open";
        const string OPERATION_ENCRYPT = "encrypt";
        const string OPERATION_DECRYPT = "decrypt";

        /// <summary>
        /// Expect args[0] to be command: "compress", "encrypt", "decrypt", "open".
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine($"Not enough args: expected 2, got {args.Length}.");
                args = new string[] { OPERATION_OPEN, "C:\\Users\\mirel\\Documents\\GitHub\\FFT-Compress-Encrypt\\data\\lamp-test_compressed.bin" };
            }
            
            try
            {
                string command = args[0];
                switch (command)
                {
                    case OPERATION_COMPRESS:
                        Console.WriteLine("Keep Percentage default?");

                        string response = Console.ReadLine();
                        if (response != null && response.Length > 0 && response[0] == 'n')
                        {
                            Console.WriteLine("Input wanted percentage:");
                            double keepPct;
                            if (!double.TryParse(Console.ReadLine(), out keepPct))
                            {
                                Console.WriteLine("Invalid. Default percentage kept");
                            }
                            else
                                Algorithms.FFT.KeepPerentage = keepPct;
                        }
                        Algorithms.FFT.CompressImageByPath(args[1]);
                        Console.WriteLine("Compressed image.");
                        break;
                    case OPERATION_OPEN:
                        Algorithms.FFT.OpenCompressedImage(args[1]);
                        Console.WriteLine("Opened image.");
                        Console.ReadKey();
                        break;
                    case OPERATION_ENCRYPT:
                        break;
                    case OPERATION_DECRYPT:
                        break;
                    default:
                        Console.WriteLine($"Command not recognized: {command}");
                        return;
                }
            } catch (Exception e)
            {
                Console.WriteLine(e); Console.ReadKey();
            }

        }

        static void createRegistry()
        {

        }
    }
}