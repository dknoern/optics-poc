using ZOSKubLib;
using HPCShared;
using System.Collections.Generic;
using System;

namespace ZOSKubApp
{
    public class TestTaskWorker : ITaskWorker
    {

        public byte[] OnTask(byte[] input)
        {

            int numberToSquare = BitConverter.ToInt32(input, 0);

            int squareOfNumber = numberToSquare * numberToSquare;

            Console.WriteLine("TestTaskWorker: square of " + numberToSquare + " is " + squareOfNumber);

            return squareOfNumber.GetBytes();
        }

    }
}