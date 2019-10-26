using System;
using ml.AIMath;

namespace ml.AI
{
    public struct Volume
    {
        private static Random _random = new Random();
        private static GaussianRandom _gaussianRandom = new GaussianRandom(_random);

        public int SX;
        public int SY;
        public int Depth;

        public double[] Weights;
        public double[] dWeights;

        public int WeightsRawLen => Weights.Length;
        public double[] WeightsRaw => Weights;
        public int dWeightsRawLen => dWeights.Length;
        public double[] dWeightsRaw => dWeights;

        public Volume(int length, double c = Double.NaN, bool gaussianRandom = true)
        {
            SX = 0;
            SY = 0;
            Depth = length;

            Weights  = new double[length];
            dWeights = new double[length];
            Fill(length,  c, gaussianRandom,
                double.IsNaN(c) && gaussianRandom ? Math.Sqrt(1.0 / length) : 0);
        }

        public Volume(int x, int y, int depth, double c = Double.NaN, bool gaussianRandom = true)
        {
            SX = x;
            SY = y;
            Depth = depth;

            var n = x * y * depth;
            Weights  = new double[n];
            dWeights = new double[n];
            Fill(n, c, gaussianRandom,
                double.IsNaN(c) && gaussianRandom ? Math.Sqrt(1.0 / n) : 0);
        }

        private void Fill(int length, double c, bool gaussianRandom, double scale)
        {
            if (double.IsNaN(c))
            {
                if(gaussianRandom)
                    for (int i = 0; i < length; i++)
                        Weights[i] = _gaussianRandom.Next();
                else
                    for (int i = 0; i < length; i++)
                        Weights[i] = _random.Next();
            }
            else
            {
                for (int i = 0; i < length; i++)
                    Weights[i] = c;
            }
        }

        public double Get(int x, int y, int depth)
        {
            return Weights[(SX * y + x) * Depth + depth];
        }

        public void Set(int x, int y, int depth, double value)
        {
            Weights[(SX * y + x) * Depth + depth] = value;
        }

        public void Add(int x, int y, int depth, double value)
        {
            Weights[(SX * y + x) * Depth + depth] += value;
        }

        public double GetGrad(int x, int y, int depth)
        {
            return dWeights[(SX * y + x) * Depth + depth];
        }

        public void SetGrad(int x, int y, int depth, double value)
        {
            dWeights[(SX * y + x) * Depth + depth] = value;
        }

        public void AddGrad(int x, int y, int depth, double value)
        {
            dWeights[(SX * y + x) * Depth + depth] += value;
        }

        public void AddVolume(Volume v, double scale = 1)
        {
            for (int i = 0; i < v.Weights.Length; i++)
                Weights[i] += v.Weights[i] * scale;
        }

        public void SetConstant(double c)
        {
            for (int i = 0; i < Weights.Length; i++)
                Weights[i] = c;
        }

        public void Print(int depth)
        {
            for (var y = 0; y < SY; y++)
            {
                for (var x = 0; x < SX; x++)
                {
                    Console.Write("{0}{1}", Get(x, y, depth), x == SX - 1 ? "\n" : ", ");
                }
            }
        }

        public Volume Clone()
        {
            var v = new Volume
            {
                SX = SX,
                SY = SY,
                Depth = Depth,
                Weights = new double[SX * SY * Depth],
                dWeights = new double[SX * SY * Depth]
            };

            Weights.CopyTo(v.Weights, 0);
            dWeights.CopyTo(v.dWeights, 0);

            return v;
        }
    }
}