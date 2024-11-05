using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class DBTest
{
    private IMongoDatabase mongoDatabase;
    MongoClient mongoClient;
    public void GetData(string url)
    {
        mongoClient = new MongoClient(url);
        mongoDatabase = mongoClient.GetDatabase("Test");
    }

    IEnumerator Write()
    {
        IMongoCollection<MyData> collection = mongoDatabase.GetCollection<MyData>("your_collection_name");
        MyData data = new MyData { Name = "Sample Name", Score = 100 };
        yield return collection.InsertOneAsync(data);
        Debug.Log("写入成功");
    }
}
public class MyData
{
    public string Name { get; set; }
    public int Score { get; set; }

}
