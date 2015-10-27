using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneApp2
{
    class Place_symylator
    {
        /*static void Main(string[] args)
        {
            double[] d = new double[] { 65.0, 220.0 };
            //Console.WriteLine("\nRandom place test demo begin\n");

            double[][] rans = randomPlace(d, 0.00000005);
            //for (int i = 0; i < rans.Length; i++)
            //{
                //Console.WriteLine(rans[i][0] + " " + rans[i][1]);
            //}
            //Console.ReadLine();
        }*/

        public static double[][] randomPlace(double[] center, double radius)
        {
            double[][] rawData = new double[20][];
            Random random = new Random();
            for (int i = 0; i < rawData.Length; i++)
            {
                double radius2 = radius * radius;
                radius2 = random.NextDouble() * radius2;
                double lg = random.NextDouble() * radius2;
                double wt = radius2 - lg;
                //Console.WriteLine("lg "+ lg + " " + "wt "+ wt);
                int siginl = randomSign(random);
                int siginw = randomSign(random);

                lg = Math.Sqrt(lg) * siginl;
                wt = Math.Sqrt(wt) * siginw;
                //Console.WriteLine("lg2 " + lg + " " + "wt2 " + wt);
                double[] cc = { (center[0] + lg), (center[1] + wt) };

                rawData[i] = cc;

            }


            return rawData;
        }

        private static int randomSign(Random random)
        {
            int i = 1;
            if (random.Next(0, 2) == 1)
                i = -1;
            return i;
        }

    }
}
