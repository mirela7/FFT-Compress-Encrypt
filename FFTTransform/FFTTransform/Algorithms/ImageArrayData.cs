using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms
{
    public class ImageArrayData<T>
    {
        public class ImageArrayElement
        {
            public ImageArrayElement(short row, short col, T value)
            {
                Row = row;
                Column = col;
                Value = value;
            }

            public short Row { get; set; }
            public short Column { get; set; }
            public T Value { get; set; }
        }

        public ImageArrayData() { }
        public ImageArrayData(int r, int c)
        {
            Rows = r;
            Cols = c;
        }


        public int Rows { get; set; } = 0;
        public int Cols { get; set; } = 0;

        public List<ImageArrayElement> Elements { get; set; } = new List<ImageArrayElement>();

        public void InitializeFromMatrix<T1>(T1[,] matrix, T[,] convertedMatrix, Predicate<T1> considered0When)
        {
            Rows = matrix.GetLength(0);
            Cols = matrix.GetLength(1);
            Elements.Clear();
            for(int i = 0; i < Rows; i++)
            {
                for(int j = 0; j < Cols; j++)
                {
                    if (!considered0When(matrix[i, j]))
                        Elements.Add(new ImageArrayElement((short)i, (short)j, convertedMatrix[i, j]));
                }
            }
        }


        public void SaveTo(string path, FourierRelatedTransform transform)
        {
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    // Write the number of items
                    bw.Write(Rows);
                    bw.Write(Cols);

                    foreach(var element in Elements)
                    {
                        bw.Write(element.Row);
                        bw.Write(element.Column);
                        transform.SerializeObject(bw, element.Value);
                        //bw.Write(element.Value);
                    }

                }
            }
        }

        public void ReadFrom(string path, FourierRelatedTransform transform)
        {
            /*using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // Get the number of rows and columns in image
                    int rows = br.ReadInt32(), cols = br.ReadInt32();
                    array = new Complex[rows, cols];

                    for (int i = 0; i < array.GetLength(0); i++)
                    {
                        for (int j = 0; j < array.GetLength(1); j++)
                        {
                            double r = br.ReadDouble(), im = br.ReadDouble();
                            array[i, j] = new Complex(r, im);
                        }
                    }

                }
            }
            return array;*/
        }
    }
}
