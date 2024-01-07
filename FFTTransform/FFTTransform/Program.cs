using Emgu.CV;
using Emgu.CV.Structure;
using FFTTransform.Utils;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;

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
            if (IsRunningAsAdmin())
            {
                Console.WriteLine("Run as admin. Creating registry...");
                createRegistry();
            }
            else
            {
                Console.WriteLine("Run as guest");
            }

            if(args.Length < 2)
            {
                Console.WriteLine($"Not enough args: expected 2, got {args.Length}.");
                args = new string[] { OPERATION_COMPRESS, "C:\\Users\\mirel\\Documents\\GitHub\\FFT-Compress-Encrypt\\data\\grayscale-lamp.png" };
            }
            
            try
            {
                string command = args[0];
                switch (command)
                {
                    case OPERATION_COMPRESS:
                        Console.WriteLine($"{OPERATION_COMPRESS}! Keep Percentage default?");

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
                        Console.WriteLine("Compression starting...");
                        var timer = new Stopwatch();
                        timer.Start();
                        Algorithms.FFT.CompressImageByPath(args[1]);

                        //B: Run stuff you want timed
                        timer.Stop();

                        TimeSpan timeTaken = timer.Elapsed;
                        string foo = "Time taken: " + timeTaken.ToString(@"m\:ss\.fff");
                        Console.WriteLine($"Compressed image. {foo}");
                        Console.ReadKey();

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
            try
            {
                string dllPath = Assembly.GetExecutingAssembly().Location;
                string exePath = $"{PathUtil.GetPathWithoutFileExtension(dllPath)}.exe";
                string exeDirectory = Path.GetDirectoryName(exePath);

                string imageShell = @"SystemFileAssociations\image\shell";
                string binKeyLocation = @"SystemFileAssociations\.bin";

                // Navigate to "Computer\HKEY_CLASSES_ROOT\SystemFileAssociations\.png\Shell"
                /*using (RegistryKey imageShellKey = Registry.ClassesRoot.OpenSubKey(imageShell, true))
                {
                    if (imageShellKey != null)
                    {
                        using (RegistryKey tffAppKey = Registry.ClassesRoot.OpenSubKey(@$"{imageShell}\TFFApp", true))
                        {
                            if (tffAppKey != null)
                            {
                                Console.WriteLine("The registry is already set.");
                                return;
                            }

                            // Create TTFApp key
                            using (RegistryKey ttfAppKey = imageShellKey.CreateSubKey("TTFApp"))
                            {
                                // Create command key
                                using (RegistryKey commandKey = ttfAppKey.CreateSubKey("command"))
                                {
                                    // Set default value of the command key
                                    commandKey.SetValue("", $"\"{exePath}\" \"{OPERATION_COMPRESS}\" \"%1\"");

                                    // Set default value of TTFApp key
                                    ttfAppKey.SetValue("", "Compress using FFTTransform");

                                    // Create and set the Icon value
                                    ttfAppKey.SetValue("Icon", @$"{exeDirectory}\fft.ico");
                                }

                                Console.WriteLine("Registry modifications completed successfully.");
                            }


                        }

                    }
                    else
                    {
                        Console.WriteLine("ImageRegistry key not found. Make sure the specified path exists.");
                    }
                }*/

                Console.WriteLine("Before open subkey");
                using (RegistryKey binShellKey = Registry.ClassesRoot.OpenSubKey(binKeyLocation, true))
                {
                    Console.WriteLine("Opened subkey");
                    Console.WriteLine(binShellKey == null);
                    if (binShellKey != null)
                    {
                        Console.WriteLine("bin extension registry exists.");
                        using (RegistryKey shellKey = binShellKey.OpenSubKey("Shell"))
                        {
                            using (RegistryKey ttfAppKey = binShellKey.OpenSubKey("TTFApp"))
                            {
                                if (ttfAppKey != null)
                                {
                                    Console.WriteLine("The registry is already set.");
                                    return;
                                }
                            }

                            using (RegistryKey ttfAppKey = binShellKey.CreateSubKey("TTFApp"))
                            {
                                using (RegistryKey commandKey = ttfAppKey.CreateSubKey("command"))
                                {
                                        // Set default value of the command key
                                    commandKey.SetValue("", $"\"{exePath}\" \"{OPERATION_OPEN}\" \"%1\"");

                                    // Set default value of TTFApp key
                                    ttfAppKey.SetValue("", "Open using FFTTransform");

                                    // Create and set the Icon value
                                    ttfAppKey.SetValue("Icon", @$"{exeDirectory}\fft.ico");

                                    Console.WriteLine("Registry modifications completed successfully.");
                                }
                            }
                        }

                    }
                    else
                    {
                        using (RegistryKey baseKey = Registry.ClassesRoot.OpenSubKey(@"SystemFileAssociations", true))
                        {
                            using (RegistryKey binKey = baseKey.CreateSubKey(".bin", true))
                            {
                                using (RegistryKey shellKey = binKey.CreateSubKey("Shell", true))
                                {
                                    using (RegistryKey ttfKey = shellKey.CreateSubKey("TTFApp", true))
                                    {
                                        using (RegistryKey commandKey = ttfKey.CreateSubKey("command", true))
                                        {
                                            if (baseKey == null)
                                                Console.WriteLine("commandKey null.");
                                            else Console.WriteLine("commandKey created");
                                            commandKey.SetValue("", $"\"{exePath}\" \"{OPERATION_OPEN}\" \"%1\"");
                                            ttfKey.SetValue("", "Open using FFTTransform");
                                            ttfKey.SetValue("Icon", @$"{exeDirectory}\fft.ico");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        static bool IsRunningAsAdmin()
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(currentIdentity);

            // Check if the current user is a member of the Administrators group
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}