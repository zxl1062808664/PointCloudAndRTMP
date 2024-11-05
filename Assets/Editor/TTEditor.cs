using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.EditorTools
{
    public class TTEditor : Editor
    {
        [MenuItem("ZZZ/ZCC")]
        public static void AA()
        {
            byte a = 1;
            byte b = 2;
            AA(a, b);
            BB(a, b);
        }

        static byte[] sizeTwoBuffer = new byte[2];
        static void BB(byte a, byte b)
        {
            sizeTwoBuffer[0] = b;
            sizeTwoBuffer[1] = a;
            ushort v = BitConverter.ToUInt16(sizeTwoBuffer);
            Debug.Log(v);

        }
        static void AA(byte a, byte b)
        {
            ushort v = (ushort)(((ushort)b << 8) | a);
            Debug.Log(v);
        }
    }
}
