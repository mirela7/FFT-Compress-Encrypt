using FFTTransform.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms.Encoder
{
    internal class HuffmanTree
    {
        internal class HuffmanNode
        {
            public JpegTriplet? Value { get; set; }
            public int Frequency { get; set; }

            public HuffmanNode? LeftChild { get; set; }
            public HuffmanNode? RightChild { get; set; }

            public HuffmanNode(JpegTriplet? value, int freq, HuffmanNode? lc, HuffmanNode? rc)
            {
                Value = value;
                Frequency = freq;
                LeftChild = lc;
                RightChild = rc;
            }

            public static HuffmanNode Leaf(JpegTriplet value, int freq)
            {
                return new HuffmanNode(value, freq, null, null);
            }

            public static HuffmanNode InternalNode(HuffmanNode lc, HuffmanNode rc)
            {
                return new HuffmanNode(null, lc.Frequency + rc.Frequency, lc, rc);
            }

            public bool IsLeaf()
            {
                return Value is not null;
            }

            public static bool operator ==(HuffmanNode obj1, HuffmanNode obj2)
            {
                return obj1.Frequency == obj2.Frequency;
            }

            public static bool operator !=(HuffmanNode obj1, HuffmanNode obj2)
            {
                return !(obj1 == obj2);
            }

            public static bool operator <(HuffmanNode obj1, HuffmanNode obj2)
            {
                return obj1.Frequency < obj2.Frequency;
            }

            public static bool operator >(HuffmanNode obj1, HuffmanNode obj2)
            {
                return obj1.Frequency > obj2.Frequency;
            }
            public static bool operator <=(HuffmanNode obj1, HuffmanNode obj2)
            {
                return !(obj1 > obj2);
            }

            public static bool operator >=(HuffmanNode obj1, HuffmanNode obj2)
            {
                return !(obj1 < obj2);
            }
        }

        public HuffmanNode Root { get; set; }
        public Dictionary<JpegTriplet, BitArray> TripletToBitArrayDict { get; set; }
        public Dictionary<BitArray, JpegTriplet> BitArrayToTripletDict { get; set; }

        public BitArray TripletsOrderedEncodings { get; set; }

        public HuffmanTree(List<JpegTriplet> triplets)
        {
            Dictionary<JpegTriplet, int> freqs = CalculateFrequency(triplets);
            PriorityQueue<HuffmanNode, int> Q = new();

            Console.WriteLine("Priority queue construction...");
            foreach(var entry in freqs)
                Q.Enqueue(HuffmanNode.Leaf(entry.Key, entry.Value), entry.Value);

            Console.WriteLine("Tree construction...");
            while(Q.Count >= 2)
            {
                HuffmanNode n1 = Q.Dequeue();
                HuffmanNode n2 = Q.Dequeue();

                Q.Enqueue(HuffmanNode.InternalNode(n1, n2), n1.Frequency + n2.Frequency);
            }
            Root = Q.Dequeue();

            TripletToBitArrayDict = new();
            Console.WriteLine("Tree traversing...");
            InitializeTripletEncodings();

            Console.WriteLine("Encoding into bit arrays...");
            List<bool> bytes = new List<bool>();
            foreach(var t in triplets)
            {
                bool[] byteArray = new bool[TripletToBitArrayDict[t].Count];
                TripletToBitArrayDict[t].CopyTo(byteArray, 0);
                bytes.AddRange(byteArray);
                //bytes.AddRange(TripletToBitArrayDict[t].OfType<byte>());
            }
            TripletsOrderedEncodings = new BitArray(bytes.ToArray());
            //TripletsOrderedEncodings = TripletsOrderedEncodings.Append(TripletToBitArrayDict[t]);
        }

        public HuffmanTree()
        {

        }

        public void WriteTreeDictionary(BinaryWriter bw)
        {
            bw.Write(TripletToBitArrayDict.Count);
            foreach (var entry in TripletToBitArrayDict)
            {
                JpegTriplet trp = entry.Key;
                bw.Write(trp.ZerosBefore);
                bw.Write(trp.Coeff);
                bw.WriteCompact(entry.Value);
            }
        }

        public void ReadTreeDictionary(BinaryReader br)
        {
            int len = br.ReadInt32();
            BitArrayToTripletDict = new(new BitArrayComparer());

            
            for(int i = 0; i < len; i++)
            {
                JpegTriplet readTriplet = new();
                readTriplet.ZerosBefore = br.ReadChar();
                readTriplet.Coeff = br.ReadInt16();
                BitArray representation = br.ReadCompact();

                BitArrayToTripletDict.Add(representation, readTriplet);
            }

        }

        public List<JpegTriplet> DecodeBitArray(BitArray bitArray)
        {
            List<JpegTriplet> result = new List<JpegTriplet>();
            //BitArray currentRepresentation = new BitArray(0);
            List<bool> bits = new List<bool>();
            for(int i = 0; i < bitArray.Length; i++)
            {
                bits.Add(bitArray[i]);
                //currentRepresentation = currentRepresentation.Append(new BitArray(1, bitArray[i]));
                BitArray currentRepresentation = new BitArray(bits.ToArray());
                if (BitArrayToTripletDict.ContainsKey(currentRepresentation))
                {
                    result.Add(new JpegTriplet(BitArrayToTripletDict[currentRepresentation]));
                    bits.Clear();
                }
            }
            return result;
        }

        public void InitializeTripletEncodings()
        {
            TreeTraverse(Root, new List<bool>() { false });
        }

        public void TreeTraverse(HuffmanNode? currentNode, List<bool> bitString)
        {
            if (currentNode is null)
                return;
            if(currentNode.IsLeaf())
            {
                bool[] byteArray = bitString.ToArray();
                TripletToBitArrayDict.Add(currentNode.Value!, new(byteArray));
                return;
            }
            bitString.Add(false);
            TreeTraverse(currentNode.LeftChild, bitString);
            bitString[bitString.Count - 1] = true;
            TreeTraverse(currentNode.RightChild, bitString);
            bitString.RemoveAt(bitString.Count - 1);

        }


        public Dictionary<JpegTriplet, int> CalculateFrequency(List<JpegTriplet> triplets)
        {
            Dictionary<JpegTriplet, int> dict = new();
            foreach (var t in triplets)
            {
                if (dict.ContainsKey(t))
                    dict[t] += 1;
                else
                    dict.Add(t, 1);
            }
            return dict;
        }
    }


    public class HuffmanCoder
    {
        public List<JpegTriplet> Triplets { get; set; } = new();
        internal HuffmanTree? Tree { get; set; }

        internal void AddRange(List<JpegTriplet> values)
        {
            Triplets.AddRange(values);
        }

        internal void Encode()
        {
            Tree = new HuffmanTree(Triplets);
        }

        internal void Decode(BitArray bitArray)
        {
            // Decodes the bitArray into List<Triplets> using HuffmanTree
            Triplets = Tree!.DecodeBitArray(bitArray);
        }

        /*internal (int, byte[]) GetBitstring()
        {
            if (Tree is null)
                throw new NullReferenceException("Called HuffmanCoder::GetBistring() before encoding data into Tree.");
            int length = Tree.TripletsOrderedEncodings.Count;
            byte[] data = new byte[length / 8 + 1];

            byte pow = 7;
            int index = 0;
            byte dataByte = 0;
            foreach(bool boolBit in Tree.TripletsOrderedEncodings)
            {
                byte bit = boolBit == true ? (byte)1 : (byte)0;
                dataByte = (byte)(dataByte | (bit<<pow));
                if(pow == 0)
                {
                    data[index++] = dataByte;
                    dataByte = 0;
                    pow = 7;
                }
                else 
                    pow--;
            }
            return (length, data);
        }*/

        internal object GetTable()
        {
            throw new NotImplementedException();
        }
    }
}
