using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MemoryPack;
using System;

public class NewBehaviourScript : MonoBehaviour
{
    public TestData testData;
    // Start is called before the first frame update
    void Start()
    {
        testData = new TestData() { id = 1, name = "???" };
        byte[] bytes = MemoryPackSerializer.Serialize(testData);
        Debug.Log(bytes.Length);

        TestData testData1 = MemoryPackSerializer.Deserialize<TestData>(bytes);
        Debug.Log(testData1.id);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            byte a = 1;
            byte b = 2;
            AA(a, b);
            BB(a, b);
        }
    }

    byte[] sizeTwoBuffer = new byte[2];
    void BB(byte a, byte b)
    {
        sizeTwoBuffer[0] = b;
        sizeTwoBuffer[1] = a;
        ushort v = BitConverter.ToUInt16(sizeTwoBuffer);
        Debug.Log(v);

    }
    void AA(byte a, byte b)
    {
        ushort v = (ushort)(((ushort)b << 8) | a);
        Debug.Log(v);
    }
}


[MemoryPackable]
public partial class TestData
{

    [MemoryPackInclude]
    public int id { get; set; }

    [MemoryPackInclude]
    public string name { get; set; }

    [MemoryPackInclude]
    public byte[] data { get; set; }

    [MemoryPackInclude]
    public TestType type { get; set; }

}
public enum TestType
{

}