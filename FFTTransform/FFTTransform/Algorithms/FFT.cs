using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using FFTTransform.Utils;
using Emgu.CV.CvEnum;
using static System.Net.Mime.MediaTypeNames;
using Emgu.CV.Dai;

namespace FFTTransform.Algorithms
{
    public class FFT : FourierRelatedTransform
    {
        public static double KeepPerentage = 0.01;

        /// <summary>
        /// Implements DFT on a vector of complex numbers
        /// </summary>
        /// <param name="vect"> the vector we want to apply the fourier transform to </param>
        /// <param name="invert"> if applying direct or inverse transformation </param>
        public static void fft_recursive(List<Complex> vect, bool invert)
        {
            int n = vect.Count;
            if (n == 1)
                return;

            List<Complex> vect_odd = new List<Complex>(); // Odd Multiples
            List<Complex> vect_even = new List<Complex>(); // Even Multiples

            for (int i = 0; 2 * i < n; i++)
            {
                vect_even.Add(vect[2 * i]);
                vect_odd.Add(vect[2 * i + 1]);
            }

            fft_recursive(vect_even, invert);
            fft_recursive(vect_odd, invert);

            double ang = 2 * Math.PI / n * (invert ? 1 : -1); // twiddle factor
            Complex w = new Complex(1, 0);
            Complex w_n = new Complex(Math.Cos(ang), Math.Sin(ang));

            for (int i = 0; 2 * i < n; i++)
            {
                vect[i] = vect_even[i] + w * vect_odd[i];
                vect[i + n / 2] = vect_even[i] - w * vect_odd[i];
                if (invert)
                {
                    /**
                     * Here we devide by 2 so in the end we divide by N:
                     * - even: (a+b)/2
                     * - odd: (c+d)/2
                     * - result: (a+b)/2 + w*(c+d)/2 = 1/4(event + w*odd) -- and we do have 4 
                     * Assumption: powers of 2
                     */
                    vect[i] /= 2;
                    vect[i + n / 2] /= 2;
                }
                w *= w_n;
            }
        }

        private static void PrintList<T>(String message, List<T> val)
        {
            Console.WriteLine(message);
            foreach (T el in val)
                Console.Write(el + " ");
            Console.WriteLine();
        }

        public static Complex[,] FFT2D(Complex[,] inputImage, bool inverse = false, bool debug = false)
        {
            Complex[,] finalImage = new Complex[inputImage.GetLength(0), inputImage.GetLength(1)];
            Complex zero = new Complex(0, 0);

            // First Apply FFT on lines
            for (int i = 0; i < inputImage.GetLength(0); i++)
            {
                List<Complex> row = Enumerable.Repeat(zero, inputImage.GetLength(1)).ToList();

                for (int j = 0; j < inputImage.GetLength(1); j++)
                {
                    row[j] = inputImage[i, j];
                }


                if (debug)
                    PrintList<Complex>("Applying FFT on row: ", row);

                fft_recursive(row, inverse);

                if (debug)
                    PrintList<Complex>("The result is: ", row);


                for (int j = 0; j < inputImage.GetLength(1); j++)
                    finalImage[i, j] = row[j];
            }

            // Apply FFT on columns
            for (int j = 0; j < inputImage.GetLength(1); j++)
            {
                List<Complex> column = Enumerable.Repeat(zero, inputImage.GetLength(0)).ToList();
                for (int i = 0; i < inputImage.GetLength(0); i++)
                {
                    column[i] = finalImage[i, j];
                }

                if (debug)
                    PrintList<Complex>("Applying FFT on column: ", column);

                fft_recursive(column, inverse);

                if (debug)
                    PrintList<Complex>("The result is: ", column);


                for (int i = 0; i < inputImage.GetLength(1); i++)
                    finalImage[i, j] = column[i];
            }

            return finalImage;
        }

        public static Complex[,] ConvertImageToComplexMatrix(Image<Gray, byte> inputImage)
        {
            Complex[,] img = new Complex[inputImage.Rows, inputImage.Cols];
            for (int i = 0; i < inputImage.Rows; i++)
            {
                for (int j = 0; j < inputImage.Cols; j++)
                {
                    img[i, j] = new Complex(inputImage.Data[i, j, 0], 0);
                }
            }
            return img;
        }

        public static Complex[,] ConvertImageToComplexMatrix(Image<Bgr, byte> inputImage, int c)
        {
            Complex[,] img = new Complex[inputImage.Rows, inputImage.Cols];
            for (int i = 0; i < inputImage.Rows; i++)
            {
                for (int j = 0; j < inputImage.Cols; j++)
                {
                    img[i, j] = new Complex(inputImage.Data[i, j, c], 0);
                }
            }
            return img;
        }

        public static Complex[,] ConvertByteToComplexMatrix(byte[,] bytes)
        {
            Complex[,] img = new Complex[bytes.GetLength(0), bytes.GetLength(1)];
            for (int i = 0; i < bytes.GetLength(0); i++)
            {
                for (int j = 0; j < bytes.GetLength(1); j++)
                {
                    img[i, j] = new Complex(bytes[i, j], 0);
                }
            }
            return img;
        }

        public static byte[,] MagnitudeOfComplexMatrix(Complex[,] image)
        {
            int rows = image.GetLength(0), cols = image.GetLength(1);
            byte[,] channel = new byte[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    channel[i, j] = (byte)image[i,j].Magnitude;
                }
            }
            return channel;
        }

        public static void SetPropertyOfComplexMatrixInChannel<T>(Complex[,] image, T[,,] channels, int c, Func<Complex, T> Property)
        {
            int rows = image.GetLength(0), cols = image.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    channels[i, j, c] = Property(image[i, j]);
                }
            }
        }

        public static byte[,] MagnitudeOfImageAsByteArray(Complex[,] image)
        {
            int rows = image.GetLength(0), cols = image.GetLength(1);
            byte[,] result = new byte[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = (byte)image[i, j].Magnitude;
                }
            }
            return result;
        }

        public static void CompressImageByPath(string p)
        {
            Console.WriteLine("Is it grayscale? Defaults to 'no'");
            string response = Console.ReadLine();
            bool isGrayscale = false;
            if(response != null && response.Length > 0 && response[0] == 'y')
                isGrayscale = true;

            Console.WriteLine("Also save magnitude and phase images? ");
            response = Console.ReadLine();
            bool debug = false;
            if (response != null && response.Length > 0 && response[0] == 'y')
                debug = true;

            Mat inputImage = CvInvoke.Imread(p);
            if (inputImage == null || inputImage.IsEmpty)
                throw new ArgumentException("Invalid path.");


            string magnitudePath = $"{PathUtil.GetPathWithoutFileExtension(p)}_magnitude.bmp";
            string phasePath = $"{PathUtil.GetPathWithoutFileExtension(p)}_phase.bmp";

            if (isGrayscale)
            {
                Image<Gray, byte> grayImage;
                Console.WriteLine("BW image compressing...");
                if (inputImage.NumberOfChannels > 1)
                    grayImage = new Image<Bgr, byte>(inputImage).Convert<Gray, byte>();
                else
                    grayImage = new(inputImage);

                Complex[,] complexImage = ConvertImageToComplexMatrix(grayImage);

                ImageArrayData<object> writableResult;
                Complex[,] fftImage;
                FFTComplexChannel(complexImage, out fftImage, out writableResult);

                string outputPath = $"{PathUtil.GetPathWithoutFileExtension(p)}_compressed.bin";
                FileReaderWriter.WriteImageArrayData(new FFT(), new ImageArrayData<object>[] { writableResult }, outputPath);

                if(debug)
                {

                    double[,,] magnitudeMatrix = new double[grayImage.Height, grayImage.Width, 1];
                    double[,,] phaseMatrix = new double[grayImage.Height, grayImage.Width, 1];
                    
                    SetPropertyOfComplexMatrixInChannel(fftImage, magnitudeMatrix, 0, (c) => c.Magnitude);
                    SetPropertyOfComplexMatrixInChannel(fftImage, phaseMatrix, 0, (c) => c.Phase);

                    double minMagnitude = Math.Log(magnitudeMatrix.Cast<double>().Min());
                    double maxMagnitude = Math.Log(magnitudeMatrix.Cast<double>().Max());
                    double minPhase = phaseMatrix.Cast<double>().Min();
                    double maxPhase = phaseMatrix.Cast<double>().Max();

                    byte[,,] byteMagnitudeMatrix = new byte[magnitudeMatrix.GetLength(0), magnitudeMatrix.GetLength(1), magnitudeMatrix.GetLength(2)];
                    byte[,,] bytePhaseMatrix = new byte[magnitudeMatrix.GetLength(0), magnitudeMatrix.GetLength(1), magnitudeMatrix.GetLength(2)];

                    TransferMatrixChannel(magnitudeMatrix, byteMagnitudeMatrix, 0, (db) => (byte)(255.0 * (Math.Log(db) - minMagnitude) / (maxMagnitude - minMagnitude)));
                    TransferMatrixChannel(phaseMatrix, bytePhaseMatrix, 0, (db) => (byte)(255.0 * (db - minPhase) / (maxPhase - minPhase)));

                    FFTShift(byteMagnitudeMatrix);

                    Image<Gray, byte> magImage = new(byteMagnitudeMatrix);
                    Image<Gray, byte> phaseImage = new(bytePhaseMatrix);

                    CvInvoke.Imwrite(magnitudePath, magImage);
                    CvInvoke.Imwrite(phasePath, phaseImage);
                }

            }
            else
            {
                //if (inputImage.Depth == DepthType.Cv8U && inputImage.NumberOfChannels == 3)
                Console.WriteLine("RGB image compressing....");
                
                string outputPath = $"{PathUtil.GetPathWithoutFileExtension(p)}_compressed.bin";
                Image<Bgr, byte> colorImage = new(inputImage);
                // Process the color image as needed

                double[,,] magnitudeMatrix = new double[colorImage.Height, colorImage.Width, 3];
                double[,,] phaseMatrix = new double[colorImage.Height, colorImage.Width, 3];


                ImageArrayData<object>[] channelsWritableResult = new ImageArrayData<object>[3];

                for (int i = 0; i <= 2; i++)
                {
                    Console.WriteLine($"Compressing channel {i}...");
                    Complex[,] complexImage = ConvertImageToComplexMatrix(colorImage, i);
                    Complex[,] fftImage;
                    FFTComplexChannel(complexImage, out fftImage, out channelsWritableResult[i]);

                    if (debug)
                    {
                        SetPropertyOfComplexMatrixInChannel(fftImage, magnitudeMatrix, i, (c) => c.Magnitude);
                        SetPropertyOfComplexMatrixInChannel(fftImage, phaseMatrix, i, (c) => c.Phase);
                    }

                }

                FileReaderWriter.WriteImageArrayData(new FFT(), channelsWritableResult, outputPath);

                if (debug)
                {
                    double minMagnitude = Math.Log(magnitudeMatrix.Cast<double>().Min());
                    double maxMagnitude = Math.Log(magnitudeMatrix.Cast<double>().Max());
                    double minPhase = phaseMatrix.Cast<double>().Min();
                    double maxPhase = phaseMatrix.Cast<double>().Max();

                    byte[,,] byteMagnitudeMatrix = new byte[magnitudeMatrix.GetLength(0), magnitudeMatrix.GetLength(1), magnitudeMatrix.GetLength(2)];
                    byte[,,] bytePhaseMatrix = new byte[magnitudeMatrix.GetLength(0), magnitudeMatrix.GetLength(1), magnitudeMatrix.GetLength(2)];

                    for (int c = 0; c < 3; c++)
                    {
                        TransferMatrixChannel(magnitudeMatrix, byteMagnitudeMatrix, c, (db) => (byte)(255.0 * (Math.Log(db) - minMagnitude) / (maxMagnitude - minMagnitude)));
                        TransferMatrixChannel(phaseMatrix, bytePhaseMatrix, c, (db) => (byte)(255.0 * (db - minPhase) / (maxPhase - minPhase)));
                    }

                    FFTShift(byteMagnitudeMatrix);


                    Image<Bgr, byte> magImage = new(byteMagnitudeMatrix);
                    Image<Bgr, byte> phaseImage = new(bytePhaseMatrix);

                    CvInvoke.Imwrite(magnitudePath, magImage);
                    CvInvoke.Imwrite(phasePath, phaseImage);
                }

            }

        }

        private static void TransferMatrixChannel<TIN, TOUT>(TIN[,,] inputMatrix, TOUT[,,] outputMatrix, int c, Func<TIN, TOUT> convertFunc) where TOUT : new()
        {
            if(outputMatrix == null)
                outputMatrix = new TOUT[inputMatrix.GetLength(0), inputMatrix.GetLength(1), inputMatrix.GetLength(2)];
            for(int i = 0; i < inputMatrix.GetLength(0); i++)
                for(int j = 0; j < inputMatrix.GetLength(1); j++)
                {
                    outputMatrix[i, j, c] = convertFunc(inputMatrix[i, j, c]);
                }
        }

        private static void FFTComplexChannel(Complex[,] complexImage, out Complex[,] outputImage, out ImageArrayData<object> writableResult)
        {
            outputImage = FFT2D(complexImage);
            // save outputImage to file. (automat ${p}_compressed.b)
            writableResult = new ImageArrayData<object>();
            writableResult.InitializeFromMatrix(outputImage, ComplexToObject(outputImage), (c) => false);
            object topMagnitudeObject = writableResult.Elements
                .OrderByDescending(i => ((Complex)i.Value).Magnitude)
                .ElementAt((int)(KeepPerentage * writableResult.Elements.Count))
                .Value;
            double topMagnitude = 0;
            if (topMagnitudeObject is Complex obj)
            {
                topMagnitude = obj.Magnitude;
            }
            else
                throw new ArgumentException("The provided object is not of type System.Numerics.Complex.");
            writableResult.InitializeFromMatrix(outputImage, ComplexToObject(outputImage), (c) => c.Magnitude <= topMagnitude);
        }

        public static void OpenCompressedImage(string p)
        {
            Complex[][,] inputImage = FileReaderWriter.ReadImageArrayData(new FFT(), p);
            byte[,,] outputImageChannels = new byte[inputImage[0].GetLength(0), inputImage[0].GetLength(1), inputImage.Length];
            for(int i = 0; i < inputImage.Length; i++)
            {
                Complex[,] outputImage = FFT2D(inputImage[i], inverse: true);
                byte[,] magnitude = MagnitudeOfComplexMatrix(outputImage);
                SetPropertyOfComplexMatrixInChannel(outputImage, outputImageChannels, i, (c) => (byte)c.Magnitude);
                //SetMagnitudeOfComplexMatrixInChannel(outputImage, outputImageChannels, i);
            }

            if(inputImage.Length == 1) // BW
            {
                Image<Gray, byte> image = new(outputImageChannels);
                string outputPath = $"{PathUtil.GetPathWithoutFileExtension(p)}_decompressed.bmp";

                CvInvoke.Imwrite(outputPath, image);
                CvInvoke.Imshow("Image", image);
                CvInvoke.WaitKey();
            }

            else if (inputImage.Length == 3) { // RGB
                Image<Bgr, byte> image = new(outputImageChannels);

                string outputPath = $"{PathUtil.GetPathWithoutFileExtension(p)}_decompressed.bmp";

                CvInvoke.Imwrite(outputPath, image);
                CvInvoke.Imshow("Image", image);
                CvInvoke.WaitKey();
            }
        }

        public override object[,] Transform(byte[,] channel)
        {
            Complex[,] complexImage = ConvertByteToComplexMatrix(channel);
            Complex[,] outputImage = FFT2D(complexImage);

            return ComplexToObject(outputImage);
        }

        public override byte[,] InverseTransform(object[,] data)
        {
            Complex[,] inputImage = ObjectToComplex(data);
            Complex[,] outputImage = FFT2D(inputImage, inverse: true);
            return MagnitudeOfImageAsByteArray(outputImage);
        }

        public override ImageArrayData<object> WriteToArrayData(object[,] channel)
        {
            Complex[,] complexData = ObjectToComplex(channel);
            ImageArrayData<object> writableResult = new ImageArrayData<object>();
            double topMagnitude = (double) writableResult.Elements
                .OrderByDescending(i => ((Complex)i.Value).Magnitude)
                .ElementAt((int)KeepPerentage*writableResult.Elements.Count)
                .Value;
            writableResult.InitializeFromMatrix(complexData, ComplexToObject(complexData), (c) => c.Magnitude <= topMagnitude);
            return writableResult;
        }

        public static void FFTShift(byte[,,] processedMatrix)
        {
            for (int i = 0; i < processedMatrix.GetLength(0) / 2; i++)
            {
                for (int j = 0; j < processedMatrix.GetLength(1); j++)
                {
                    for(int c = 0; c < processedMatrix.GetLength(2); c++)
                    {
                        byte aux = processedMatrix[i, j, c];
                        processedMatrix[i, j, c] = processedMatrix[i + processedMatrix.GetLength(0) / 2, j, c];
                        processedMatrix[i + processedMatrix.GetLength(0) / 2, j, c] = aux;
                    }
                    
                }
            }

            for (int i = 0; i < processedMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < processedMatrix.GetLength(1) / 2; j++)
                {
                    for(int c = 0; c < processedMatrix.GetLength(2); c++)
                    {
                        byte aux = processedMatrix[i, j, c];
                        processedMatrix[i, j, c] = processedMatrix[i, j + processedMatrix.GetLength(1) / 2, c];
                        processedMatrix[i, j + processedMatrix.GetLength(1) / 2, c] = aux;
                    }
                }
            }
        }

        public override object[,] ReadFromArrayData(ImageArrayData<object> arrayData)
        {
            throw new NotImplementedException();
        }

        #region Conversions
        private static object[,] ComplexToObject(Complex[,] complexArray)
        {
            object[,] objectArray = new object[complexArray.GetLength(0), complexArray.GetLength(1)];
            for (int i = 0; i < complexArray.GetLength(0); i++)
            {
                for (int j = 0; j < complexArray.GetLength(1); j++)
                {
                    objectArray[i, j] = complexArray[i, j];
                }
            }
            return objectArray;

        }

        private static Complex[,] ObjectToComplex(object[,] objectsArray)
        {
            Complex[,] complexArray = new Complex[objectsArray.GetLength(0), objectsArray.GetLength(1)];
            for (int i = 0; i < objectsArray.GetLength(0); i++)
            {
                for (int j = 0; j < objectsArray.GetLength(1); j++)
                {
                    complexArray[i, j] = (Complex)objectsArray[i, j];
                }
            }
            return complexArray;

        }

        public override void SerializeObject<T>(BinaryWriter bw, T obj)
        {
            if (obj is Complex complexObj)
            {
                bw.Write(complexObj.Real);
                bw.Write(complexObj.Imaginary);
            }
            else
            {
                throw new ArgumentException("The provided object is not of type System.Numerics.Complex.");
            }
        }

        public override object DeserializeObject(BinaryReader br)
        {
            double real = br.ReadDouble(), im = br.ReadDouble();
            Complex val = new Complex(real, im);
            return val;
        }
        #endregion


    }
}
