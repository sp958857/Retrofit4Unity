using System.IO;
using UnityEngine;

namespace Retrofit
{
    public class MultipartBody
    {
        public static readonly string FIELD_FILE = "file";
        public static readonly string MIME_TYPE_STREAM = "application/octet-stream";
        private static long DEFAULT_MAX_FILE_SIZE = 5242880L;

        //    field,fileName,MIME-TYPE

        public string Field;
        public string Mimetype;
        public string FileName;
        public FileInfo FileInfo;

        private byte[] rawData;



        public MultipartBody(FileInfo fileInfo)
        {
            this.FileInfo = fileInfo;
            FileName = fileInfo.Name;
            Field = FIELD_FILE;
            Mimetype = MIME_TYPE_STREAM;
        }

        public MultipartBody(FileInfo fileInfo, string fileName, string field, string mimetype)
        {
            this.FileInfo = fileInfo;
            FileName = fileName;
            Field = field;
            Mimetype = mimetype;
        }

        public MultipartBody(byte[] data, string fileName, string field, string mimetype)
        {
            rawData = data;
            FileName = fileName;
            Field = field;
            Mimetype = mimetype;
        }


        public byte[] GetBinaryData()
        {
            if (rawData != null)
            {
                return rawData;
            }
            else
            {
                if (FileInfo != null)
                {
                    if (FileInfo.Length > DEFAULT_MAX_FILE_SIZE)
                    {
                        Debug.LogWarning("File size is bigger than 5M, recommand to upload by chunks highly");
                    }
                    var data = File.ReadAllBytes(FileInfo.FullName);
                    return data;
                }
                else
                {
                    return null;
                }
            }
        }

        public static MultipartBody Convert(object part)
        {
            var f = part as FileInfo;
            if (f != null)
            {
                return new MultipartBody((FileInfo) part);
            }
            else if (part is MultipartBody)
            {
                return (MultipartBody) part;
            }
            else
            {
                return null;
            }
        }
    }
}