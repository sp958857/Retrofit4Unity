using System;
using System.IO;
using System.Net;
using System.Text;
using CI.HttpClient;
using UnityEngine;
using UnityEngine.UI;

public class ExampleSceneManagerController : MonoBehaviour
{
    public Text LeftText;
    public Text RightText;
    public Slider ProgressSlider;
    public void UploadFile()
    {
        HttpClient client = new HttpClient();
        client.Proxy = new WebProxy(new Uri("http://127.0.0.1:8888"));

        FileInfo fileInfo = new FileInfo(@"D:\error.png");
        byte[] buffer = File.ReadAllBytes(fileInfo.FullName);
        ByteArrayContent content = new ByteArrayContent(buffer, "application/octet-stream");
        MultipartFormDataContent multipart = new MultipartFormDataContent();
        multipart.Add(content,"file",fileInfo.Name);
        ProgressSlider.value = 0;
        client.Post(new System.Uri("http://httpbin.org/post"), multipart, HttpCompletionOption.AllResponseContent, (r) =>
        {
        }, (u) =>
        {
            LeftText.text = "Upload: " + u.PercentageComplete.ToString() + "%";
            ProgressSlider.value = u.PercentageComplete;
        });
    }
    public void Upload()
    {
        HttpClient client = new HttpClient();
        client.Proxy = new WebProxy(new Uri("http://127.0.0.1:8888"));

//        byte[] buffer = new byte[1000000];
        byte[] buffer = new byte[1000];
        new System.Random().NextBytes(buffer);

        ByteArrayContent content = new ByteArrayContent(buffer, "application/bytes");

        ProgressSlider.value = 0;

        client.Post(new System.Uri("http://httpbin.org/post"), content, HttpCompletionOption.AllResponseContent, (r) =>
        {           
        }, (u) =>
        {
            LeftText.text = "Upload: " +  u.PercentageComplete.ToString() + "%";
            ProgressSlider.value = u.PercentageComplete;
        });
    }

    public void Download()
    {
        HttpClient client = new HttpClient();

        ProgressSlider.value = 100;

        client.GetByteArray(new System.Uri("http://download.thinkbroadband.com/5MB.zip"), HttpCompletionOption.StreamResponseContent, (r) =>
        {
            RightText.text = "Download: " + r.PercentageComplete.ToString() + "%";
            ProgressSlider.value = 100 - r.PercentageComplete;
        });
    }

    public void UploadDownload()
    {
        HttpClient client = new HttpClient();

        byte[] buffer = new byte[1000000];
        new System.Random().NextBytes(buffer);

        ByteArrayContent content = new ByteArrayContent(buffer, "application/bytes");

        ProgressSlider.value = 0;

        client.Post(new System.Uri("http://httpbin.org/post"), content, HttpCompletionOption.StreamResponseContent, (r) =>
        {
            RightText.text = "Download: " + r.PercentageComplete.ToString() + "%";
            ProgressSlider.value = 100 - r.PercentageComplete;
        }, (u) =>
        {
            LeftText.text = "Upload: " + u.PercentageComplete.ToString() + "%";
            ProgressSlider.value = u.PercentageComplete;
        });
    }

    public void Delete()
    {
        HttpClient client = new HttpClient();
        client.Delete(new System.Uri("http://httpbin.org/delete"), HttpCompletionOption.AllResponseContent, (r) =>
        {
        });
    }

    public void Get()
    {
        HttpClient client = new HttpClient();
        client.GetByteArray(new System.Uri("http://httpbin.org/get"), HttpCompletionOption.AllResponseContent, (r) =>
        {
        });
    }

    public void Patch()
    {
        HttpClient client = new HttpClient();

        StringContent content = new StringContent("Hello World");

        client.Patch(new System.Uri("http://httpbin.org/patch"), content, HttpCompletionOption.AllResponseContent, (r) =>
        {
        });
    }

    public void Post()
    {
        HttpClient client = new HttpClient();

        StringContent content = new StringContent("Hello World");

        client.Post(new System.Uri("http://httpbin.org/post"), 
            content, HttpCompletionOption.AllResponseContent, (r) =>
        {
        });
    }

    public void Put()
    {
        HttpClient client = new HttpClient();
        client.Proxy = new WebProxy(new Uri("http://127.0.0.1:8888"));

        StringContent content = new StringContent("Hello World");

        client.Put(new System.Uri("http://httpbin.org/put"), content, HttpCompletionOption.AllResponseContent, (r) =>
        {
        });
    }
}