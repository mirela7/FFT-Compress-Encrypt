﻿using Emgu.CV;
using Emgu.CV.ML.MlEnum;
using Emgu.CV.Structure;
using FFTTransform.Algorithms;
using FFTTransform.Algorithms.Encoder;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Utils
{

    using HuffmanPair = System.Tuple<object, object>;
    internal class FileReaderWriter
    {
        public static void WriteComplexArray(Complex[,] array, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    // Write the number of items
                    bw.Write(array.GetLength(0));
                    bw.Write(array.GetLength(1));
                    for(int i = 0; i < array.GetLength(0); i++)
                    {
                        for(int j = 0; j < array.GetLength(1); j++)
                        {
                            bw.Write(array[i,j].Real);
                            bw.Write(array[i,j].Imaginary);
                        }
                    }
                }
            }
        }

        public static void WriteImage(Image<Gray, byte> array, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    // Write the number of items
                    bw.Write(array.Rows);
                    bw.Write(array.Cols);

                    for (int i = 0; i < array.Rows; i++)
                    {
                        for (int j = 0; j < array.Cols; j++)
                        {
                            bw.Write(array.Data[i,j,0]);
                           // bw.Write(array[i, j].Imaginary);
                        }
                    }
                }
            }
        }

        public static Complex[,] ReadComplexArray(string path)
        {
            Complex[,] array;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // Write the number of items
                    int rows = br.ReadInt32(), cols = br.ReadInt32();
                    array = new Complex[rows, cols];

                    for (int i = 0; i < array.GetLength(0); i++)
                    {
                        for (int j = 0; j < array.GetLength(1); j++)
                        {
                            double r = br.ReadDouble(), im = br.ReadDouble();
                            array[i,j] = new Complex(r, im);
                        }
                    }
                 
                }
            }
            return array;
        }

        internal static void Save(string path, ImageArrayData<object>[,,] data, FourierRelatedTransform transform)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    // Write the number of items
                    /*bw.Write(array.Rows);
                    bw.Write(array.Cols);

                    for (int i = 0; i < array.Rows; i++)
                    {
                        for (int j = 0; j < array.Cols; j++)
                        {
                            bw.Write(array.Data[i, j, 0]);
                            // bw.Write(array[i, j].Imaginary);
                        }
                    }*/
                }
            }
        }

        internal static void WriteImageArrayData(FourierRelatedTransform t, ImageArrayData<object>[] imageArray, string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(imageArray.Length);
                    for (int i = 0; i < imageArray.Length; i++)
                    {
                        // Write the number of items
                        bw.Write(imageArray[i].Rows);
                        bw.Write(imageArray[i].Cols);
                        bw.Write(imageArray[i].Elements.Count);

                        foreach (var element in imageArray[i].Elements)
                        {
                            bw.Write(element.Row);
                            bw.Write(element.Column);
                            t.SerializeObject(bw, element.Value);
                        }
                    }
                }
            }
        }

        public static Complex[][,] ReadImageArrayData(FourierRelatedTransform t, string path)
        {
            Complex[][,] array;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    int channels = br.ReadInt32();
                    array = new Complex[channels][,];

                    for(int channel = 0; channel < channels; channel++)
                    {
                        int rows = br.ReadInt32(), cols = br.ReadInt32();
                        array[channel] = new Complex[rows, cols];

                        int count = br.ReadInt32();

                        for (int i = 0; i < count; i++)
                        {
                            short r = br.ReadInt16(), c = br.ReadInt16();
                            object val = t.DeserializeObject(br);
                            if (val is Complex complex)
                            {
                                array[channel][r, c] = complex;
                            }
                            else
                            {
                                Console.WriteLine("Warning! Type not recognized.");
                            }
                        }
                    }
                }
            }
            return array;
        }

        internal static void WriteJpeg(string path, int width, int height, HuffmanCoder dcY, HuffmanCoder dcC, HuffmanCoder acY, HuffmanCoder acC)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(width);
                    bw.Write(height);

                    HuffmanCoder[] encodings = new HuffmanCoder[] { dcY, dcC, acY, acC };
                    foreach (HuffmanCoder en in encodings)
                    {
                        BitArray array = en.Tree!.TripletsOrderedEncodings;

                        en.Tree.WriteTreeDictionary(bw);
                        bw.WriteCompact(array);
                    }

                }
            }
        }

        internal static void ReadJpeg(string path, out int width, out int height, out HuffmanCoder dcY, out HuffmanCoder dcC, out HuffmanCoder acY, out HuffmanCoder acC)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    width = br.ReadInt32();
                    height = br.ReadInt32();

                    HuffmanCoder[] encodings = new HuffmanCoder[4];
                    for (int i = 0; i < encodings.Length; i++)
                    {
                        encodings[i] = new HuffmanCoder();
                        encodings[i].Tree = new HuffmanTree();

                        encodings[i].Tree!.ReadTreeDictionary(br);
                        encodings[i].Decode(br.ReadCompact());
                    }

                    dcY = encodings[0];
                    dcC = encodings[1];
                    acY = encodings[2];
                    acC = encodings[3];

                }
            }
        }

    }
}
