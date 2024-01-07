using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms
{
    internal class Quantization
    {

        public enum QuantizationType {
            YQUANTIZATION,
            CQUANTIZATION
        }

        private static int[,] luminanceMatrix =
        {
            { 16,11,10,16,24,40,51,61},
            { 12,12,14,19,26,58,60,55},            {14,13,16,24,40,57,69,56},
            {14,17,22,29,51,87,80,62},
            {18,22,37,56,68,109,103,77},
            {24,36,55,64,81,104,113,92},
            {49,64,78,87,103,121,120,101},
            {72,92,95,98,112,100,103,99 }
        };

        private static int[,] chrominanceMatrix =
        {
            { 17,18,24,47,99,99,99,99},
            {18,21,26,66,99,99,99,99},
            {24,26,56,99,99,99,99,99},
            {47,66,99,99,99,99,99,99},
            {99,99,99,99,99,99,99,99},
            {99,99,99,99,99,99,99,99},
            {99,99,99,99,99,99,99,99},
            { 99,99,99,99,99,99,99,99 }
        };

        private static int[,] GetQuantizedMatrix(QuantizationType type)
        {
            return type == QuantizationType.YQUANTIZATION ? luminanceMatrix : chrominanceMatrix;
        }

        public static int[,] Quantize(double[,] inputImage, QuantizationType type)
        {

            int[,] quantMatrix = GetQuantizedMatrix(type);
            int[,] finalImage = new int[inputImage.GetLength(0), inputImage.GetLength(1)];
            for(int i=0; i<inputImage.GetLength(0); i++)
            {
                for (int j = 0; j < inputImage.GetLength(1); j++)
                    finalImage[i, j] = (int)Math.Round(inputImage[i, j] / quantMatrix[i, j]);
            }
            return finalImage;
        }
    }
}
