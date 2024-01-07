using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms
{
    public class DCT
    {
        public DCT() { }

        public double[,] Transform(double[,] matrix, bool inverse)
        { 
            return DCT2D(matrix, inverse);
        }

        public static double[] dct(double[] row)
        {
            double[] y = new double[row.Length];
            int N = row.Length;
            if (N == 1)
            {
                y[0] = row[0];
                return y;
            }
            List<Complex> row_for_fft = Enumerable.Repeat(new Complex(0, 0), row.Length).ToList();

            for (int i = 0; i < N; i++)
            {
                int newIndex = (i % 2 == 0) ? i / 2 : N - 1 - (i / 2);
                row_for_fft[newIndex] = row[i];
            }

            //fft_recursive(row_for_fft, false);
            FFT.fft(row_for_fft, false, FFT.GetPermutationOfIndices(N));
            // normalization constant
            for (int i = 0; i < N; i++)
                row_for_fft[i] /= Math.Sqrt(row.Length);

            // calculate result
            for (int i = 0; i < N; i++)
            {
                double rp = row_for_fft[i].Real, ip = row_for_fft[i].Imaginary;
                y[i] = Math.Cos(Math.PI * i / (2 * N)) * rp + Math.Sin(Math.PI * i / (2 * N)) * ip;
                if (i >= 1)
                    y[i] = y[i] * Math.Sqrt(2);
            }

            return y;

        }

        public static double[,] ConvertImageToDoubleMatrix(byte[,] inputImage)
        {
            double[,] img = new double[inputImage.GetLength(0), inputImage.GetLength(1)];
            for (int i = 0; i < inputImage.GetLength(0); i++)
            {
                for (int j = 0; j < inputImage.GetLength(1); j++)
                {
                    img[i, j] = inputImage[i, j];
                }
            }
            return img;
        }

        public static double[] idct(double[] y)
        {
            double[] x = new double[y.Length];
            int N = y.Length;
            if (N == 1)
            {
                x[0] = y[0];
                return x;
            }

            // create Z
            List<Complex> z = Enumerable.Repeat(new Complex(0, 0), N).ToList();
            Complex j = new Complex(0, 1);
            z[0] = y[0];
            for (int i = 1; i < N; i++)
            {
                // the constant from Q:
                z[i] = Complex.Exp(Math.PI * j * i / (2 * N)) / Math.Sqrt(2);
                // the part depending on y:
                z[i] *= (y[i] - j * y[N - i]);
            }
            FFT.fft(z, invert: true, FFT.GetPermutationOfIndices(N));

            // normalize
            for (int i = 0; i < N; i++)
                z[i] *= Math.Sqrt(N);

            for (int i = 0; i < N; i += 2)
                x[i] = z[i / 2].Real;
            for (int i = 1; i < N; i += 2)
                x[i] = z[N - i / 2 - 1].Real;
            return x;
        }


        

        public static double[,] DCT2D(double[,] inputImage, bool invert = false)
        {
            double[,] finalImage = new double[inputImage.GetLength(0), inputImage.GetLength(1)];
            // First Apply FFT on lines
            for (int i = 0; i < inputImage.GetLength(0); i++)
            {
                double[] row = new double[inputImage.GetLength(1)];
                for (int j = 0; j < inputImage.GetLength(1); j++)
                    row[j] = inputImage[i, j];


                double[] res = invert ? idct(row) : dct(row);

                for (int j = 0; j < inputImage.GetLength(1); j++)
                    finalImage[i, j] = res[j];
            }

            // Apply FFT on columns
            for (int j = 0; j < inputImage.GetLength(1); j++)
            {
                double[] column = new double[inputImage.GetLength(0)];

                for (int i = 0; i < inputImage.GetLength(0); i++)
                {
                    column[i] = finalImage[i, j];
                }

                double[] res = invert ? idct(column) : dct(column);

                for (int i = 0; i < inputImage.GetLength(1); i++)
                    finalImage[i, j] = res[i];
            }

            return finalImage;
        }
    }
}
