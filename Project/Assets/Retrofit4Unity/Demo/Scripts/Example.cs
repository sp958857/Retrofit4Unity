using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading;
using Demo.Scripts;
using Retrofit;
using UniRx;
using UnityEngine.UI;

public class Example : MonoBehaviour
{

    public Button buttonGet;
    public Button buttonPost;
    public Button buttonPostBody;
    public Button buttonMultipartFileUpload;
    public Button buttonPatch;
    public Button buttonPut;
    public Button buttonDelete;
    public Button buttonPathTest;
    public Slider sldSeconds;

    public ShowResponseArg arg1;
    public ShowResponseArg arg2;
	// Use this for initialization
	void Start () {
	
        buttonGet.onClick.AddListener(OnGet);
	    buttonPost.onClick.AddListener(OnPost);
	    buttonPostBody.onClick.AddListener(OnPostBody);
	    buttonMultipartFileUpload.onClick.AddListener(OnMultipartFileUpload);
	    buttonPatch.onClick.AddListener(OnPatch);
	    buttonPut.onClick.AddListener(OnPut);
	    buttonDelete.onClick.AddListener(OnDelete);
	    buttonPathTest.onClick.AddListener(OnPathTest);
	}

    public void RestResponsePanel()
    {
        arg1.Reset();
        arg2.Reset();
    }
    private void OnGet()
    {
        RestResponsePanel();
        var ob = HttpBinService.Instance.Get("abc", "123");
        ob.SubscribeOn(Scheduler.ThreadPool)
            .ObserveOn(Scheduler.MainThread)
            .Subscribe(data =>
                {
                    Debug.LogFormat("Received on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                    arg1.ShowArg("queryArg1",data.queryArgs.arg1);
                    arg2.ShowArg("queryArg2",data.queryArgs.arg2);
                }, // onSuccess
                error =>
                {
                    Debug.Log("Retrofit Error:" + error);
                });
    }

    private void OnPost()
    {
        RestResponsePanel();
        var ob = HttpBinService.Instance.Post(123.456f, "abc");
        ob.SubscribeOn(Scheduler.ThreadPool)
            .ObserveOn(Scheduler.MainThread)
            .Subscribe(data =>
                {
                    Debug.LogFormat("Received on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                    arg1.ShowArg("form-data-field1", data.formData.arg1);
                    arg2.ShowArg("form-data-field2", data.formData.arg2);
                }, // onSuccess
                error =>
                {
                    Debug.Log("Retrofit Error:" + error);
                });
    }

    private void OnPostBody()
    {
        RestResponsePanel();
        var body = new PostBody("sp958857","China");
        var ob = HttpBinService.Instance.PostBody(body,"Unity-Client");
        ob.SubscribeOn(Scheduler.ThreadPool)
            .ObserveOn(Scheduler.MainThread)
            .Subscribe(data =>
                {
                    Debug.LogFormat("Received on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                    arg1.ShowArg("json-body", data.data);
                }, // onSuccess
                error =>
                {
                    Debug.Log("Retrofit Error:" + error);
                });
    }

    private void OnMultipartFileUpload()
    {
        RestResponsePanel();
        string filePath = Application.dataPath + "/Retrofit4Unity/Demo/Textures/error.png";
        FileInfo fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            Debug.LogError("File not exits, please locate the file to upload");
            return;
        }
        MultipartBody multipartBody = new MultipartBody(fileInfo);
        var ob = HttpBinService.Instance.MultipartFileUpload(multipartBody, 123.45f,"abc");
        ob.SubscribeOn(Scheduler.ThreadPool)
            .ObserveOn(Scheduler.MainThread)
            .Subscribe(data =>
                {
                    Debug.LogFormat("Received on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                    arg1.ShowArg("file in binary", data.files.file);
                    arg2.ShowArg("additional-form-data", data.formData.arg1);
                }, // onSuccess
                error =>
                {
                    Debug.Log("Retrofit Error:" + error);
                });
    }

    private void OnPatch()
    {
        RestResponsePanel();
        var ob = HttpBinService.Instance.Patch(123.456f);
        ob.SubscribeOn(Scheduler.ThreadPool)
            .ObserveOn(Scheduler.MainThread)
            .Subscribe(data =>
                {
                    Debug.LogFormat("Received on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                    arg1.ShowArg("form-data-field1", data.formData.arg1);
                }, // onSuccess
                error =>
                {
                    Debug.Log("Retrofit Error:" + error);
                });
    }

    private void OnPut()
    {
        RestResponsePanel();
        var ob = HttpBinService.Instance.Put(123.456f, "abc");
        ob.SubscribeOn(Scheduler.ThreadPool)
            .ObserveOn(Scheduler.MainThread)
            .Subscribe(data =>
                {
                    Debug.LogFormat("Received on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                    arg1.ShowArg("form-data-field1", data.formData.arg1);
                    arg2.ShowArg("form-data-field2", data.formData.arg2);
                }, // onSuccess
                error =>
                {
                    Debug.Log("Retrofit Error:" + error);
                });
    }

    private void OnDelete()
    {
        RestResponsePanel();
        var ob = HttpBinService.Instance.Delete();
        ob.SubscribeOn(Scheduler.ThreadPool)
            .ObserveOn(Scheduler.MainThread)
            .Subscribe(data =>
                {
                    Debug.LogFormat("Received on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                    arg1.ShowArg("original IP", data.originIp);
                }, // onSuccess
                error =>
                {
                    Debug.Log("Retrofit Error:" + error);
                });
    }

    private void OnPathTest()
    {
        RestResponsePanel();
        int s = (int) sldSeconds.value;
        var ob = HttpBinService.Instance.PathTest(s);
        ob.SubscribeOn(Scheduler.ThreadPool)
            .ObserveOn(Scheduler.MainThread)
            .Subscribe(data =>
                {
                    Debug.LogFormat("Received on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                    arg1.ShowArg("Request url", data.url);
                }, // onSuccess
                error =>
                {
                    Debug.Log("Retrofit Error:" + error);
                });
    }

}
