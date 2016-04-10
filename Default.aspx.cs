/*******************************************************************************
* Copyright 2011-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache-2-0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/

using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.OleDb;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using Amazon.S3;
using Amazon.S3.Model;
using MySql.Data.MySqlClient;

using System.Collections.Generic;

public partial class _Default : System.Web.UI.Page
{
 
   public string PhotoGalleryBaseUrl = "http://s3.amazonaws.com";
//  public string PhotoGalleryBaseUrl = "http://s3-website-us-east-1.amazonaws.com";
    public string WebsiteRootPhysicalDir = null;
    public string UploadPhysicalPath = null;
    public string GalleryPhysicalPath = null;
    private string FileThumbName = "";
    private RefreshCatcher refreshCatcher = null;

    private const string ImageBucketName ="spade-bucket"; 

    private string dbinstance = "my-db-instance2.c0eaw1nvmzgk.us-east-1.rds.amazonaws.com";

//Response.Write ("name : " + System.Configuration.ConfigurationManager.AppSettings["MySqlUserName"]);
//move pass to web.config
    private string userid = System.Configuration.ConfigurationManager.AppSettings["MySqlUserName"]; 
    private string password = System.Configuration.ConfigurationManager.AppSettings["MySqlPassWord"]; 
    private string database = "my_database"; 

    protected void Page_Load(object sender, EventArgs e)
    {
        InitializeVariables();
        CreateTable();
        refreshCatcher = new RefreshCatcher(this);

//          Response.Write("<div class=\"float\">\n");
  //          Response.Write("Upload to " + UploadPhysicalPath + "<br>filename:" + UploadFile.FileName + "<br>photogallerybase" + PhotoGalleryBaseUrl + "<br>bucketname" + ImageBucketName);
    //Response.Write("</div>\n");
   
    }

    public void DisplayRecords()
    {
        DataTable dt = new DataTable();

        // Load data table from RDS, in order that existing code requires fewer modifications
        dt = DataTableFromRDS(dt);

        for (Int16 i = 0; i < dt.Rows.Count; i++)
        {
            Response.Write("<div class=\"float\">\n");
            Response.Write("<a target=_blank href=\"" + PhotoGalleryBaseUrl + "/" + ImageBucketName + "/" + dt.Rows[i]["image_file_name"].ToString() + "\">");
            Response.Write("<img src=\"" + PhotoGalleryBaseUrl + "/" + ImageBucketName + "/" + dt.Rows[i]["image_thumb_name"].ToString() + "\" />");
            Response.Write("</a><br />\n");
            Response.Write(dt.Rows[i]["image_file_name"].ToString() + "<br />\n");
            Response.Write("Image Size: " + dt.Rows[i]["image_size"].ToString() + "<br />\n");
            Response.Write("</div>\n");
        }
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        if (refreshCatcher.IsRefresh != true)
        {
            if (SaveUploadFileToTempDir() == true)
            {
                FileThumbName = ThumbNameFromFileName(UploadFile.FileName);
                CreateThumbnail(UploadPhysicalPath, UploadFile.FileName);
//Response.Write ("baseurl should be like http://sqs.eu-west-1.amazonaws.com : " + PhotoGalleryBaseUrl );
//Response.Write ("<br><br>bucket " + ImageBucketName);
//Response.Write ("<br> UploadPhysicalPath " + UploadPhysicalPath);
//Response.Write ("<br>filename " + UploadFile.FileName);
//Response.Write ("<br><br> path sent :  " + UploadPhysicalPath + UploadFile.FileName);

                UploadS3Object(ImageBucketName, UploadFile.FileName);
                UploadS3Object(ImageBucketName, FileThumbName);
                
                AddRDSEntry();
                DeleteLocalFile(UploadPhysicalPath, FileThumbName);
                DeleteLocalFile(UploadPhysicalPath, UploadFile.FileName);
            }
        }
    }


    bool SaveUploadFileToTempDir()
    {
        string fileType = UploadFile.PostedFile.ContentType.ToString();
        bool bResult = false;
        if (UploadFile.PostedFile != null)
        {
            try
            {
                // If file exists, it will be replaced
                UploadFile.PostedFile.SaveAs(UploadPhysicalPath + UploadFile.FileName);
                UploadFile.Dispose();
                Message.Text = "";
                bResult = true;
            }
            catch (Exception exc)
            {
                Message.Text = "Upload to " + UploadPhysicalPath + UploadFile.FileName
                    + " failed.<br />Upload Path is: " + UploadPhysicalPath + "<br />"
                    + "File Name: " + UploadFile.PostedFile.FileName + "<br>"
                    + "<pre>" + exc.ToString() + "</pre>";
            }
        }
        return bResult;
    }

    private void CreateTable()
    {
        string cnstr = "server=";
        cnstr += dbinstance + ";User Id=";
        cnstr += userid + ";password=";
        cnstr += password + ";database=";
        cnstr += database;
        MySqlConnection cn = new MySqlConnection(cnstr);
        MySqlCommand cmd = new MySqlCommand();
        
        string sSql = "";
        sSql = "CREATE TABLE images (image_file_name TEXT, image_size DOUBLE, image_content_type TEXT, image_thumb_name TEXT) ";
        
        cmd.Connection = cn;
        cmd.CommandText = sSql;
        cmd.Connection.Open();
        // added try/catch if the table is already created, the program will continue
        try
        {
            cmd.ExecuteNonQuery();
        }
        catch
        {

        }

        cmd.Connection.Close();
    }

    private void AddRDSEntry()
    {
        string cnstr = "server=";
        cnstr += dbinstance + ";User Id=";
        cnstr += userid + ";password=";
        cnstr += password + ";database=";
        cnstr += database;
        MySqlConnection cn = new MySqlConnection(cnstr);
        MySqlCommand cmd = new MySqlCommand();
        
        string sSql = "";
        sSql = "INSERT INTO images (image_file_name, image_size, image_content_type, image_thumb_name) ";
        sSql += "VALUES (";
        sSql += "'" + UploadFile.FileName.ToString() + "', ";
        sSql += UploadFile.PostedFile.ContentLength + ", ";
        sSql += "'" + UploadFile.PostedFile.ContentType.ToString() + "', ";
        sSql += "'" + FileThumbName + "'";
        sSql += ")";
        
        cmd.Connection = cn;
        cmd.CommandText = sSql;
        cmd.Connection.Open();
        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    private void DeleteLocalFile(string LocalDir, string FileName)
    {
        File.Delete(LocalDir + FileName);
    }

    private void CreateThumbnail(string Directory, string FileName)
    {
        // There is a GetThumbnailImage method that is simpler; however image quality is lower
        System.Drawing.Image imgOriginal = System.Drawing.Image.FromFile(UploadPhysicalPath + FileName);
        System.Drawing.Image imgThumb = FixedSize(imgOriginal, 150, 150);
        imgThumb.Save(Directory + FileThumbName, ImageFormatFromContentType(UploadFile.PostedFile.ContentType));
        imgThumb.Dispose();
        imgOriginal.Dispose();
    }

    static System.Drawing.Image FixedSize(System.Drawing.Image imgPhoto, int Width, int Height)
    {
        int sourceWidth = imgPhoto.Width;
        int sourceHeight = imgPhoto.Height;
        int sourceX = 0;
        int sourceY = 0;
        int destX = 0;
        int destY = 0;

        float nPercent = 0;
        float nPercentW = 0;
        float nPercentH = 0;

        nPercentW = ((float)Width / (float)sourceWidth);
        nPercentH = ((float)Height / (float)sourceHeight);
        if (nPercentH < nPercentW)
        {
            nPercent = nPercentH;
            destX = System.Convert.ToInt16((Width -
                          (sourceWidth * nPercent)) / 2);
        }
        else
        {
            nPercent = nPercentW;
            destY = System.Convert.ToInt16((Height -
                          (sourceHeight * nPercent)) / 2);
        }

        int destWidth = (int)(sourceWidth * nPercent);
        int destHeight = (int)(sourceHeight * nPercent);

        Bitmap bmPhoto = new Bitmap(Width, Height,
                          PixelFormat.Format24bppRgb);
        bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                         imgPhoto.VerticalResolution);

        Graphics grPhoto = Graphics.FromImage(bmPhoto);
        grPhoto.Clear(Color.White);
        grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        grPhoto.DrawImage(imgPhoto,
            new Rectangle(destX, destY, destWidth, destHeight),
            new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
            GraphicsUnit.Pixel);

        grPhoto.Dispose();
        return bmPhoto;
    }

    public string ThumbNameFromFileName(string fileName)
    {
        int i = fileName.LastIndexOf(".");
        string baseFileName = fileName.Substring(0, i);
        int iStartPos = i + 1;
        string fileExtension = fileName.Substring(iStartPos, fileName.Length - iStartPos);
        string newFileName = baseFileName + "_thumb." + fileExtension;
        return newFileName;
    }

    private ImageFormat ImageFormatFromContentType(string ContentType)
    {
        // TODO: Add logic for additional image types, as appropriate for your site
        ImageFormat format;
        switch (ContentType)
        {
            case "image/bmp":
                format = ImageFormat.Bmp;
                break;
            case "image/gif":
                format = ImageFormat.Gif;
                break;
            case "image/pjpeg":
            case "image/jpeg":
                format = ImageFormat.Jpeg;
                break;
            case "image/png":
                format = ImageFormat.Png;
                break;
            default:
                format = ImageFormat.Jpeg;
                break;
        }
        return format;
    }

    private void InitializeVariables()
    {
        WebsiteRootPhysicalDir = System.AppDomain.CurrentDomain.BaseDirectory;
        UploadPhysicalPath = WebsiteRootPhysicalDir + @"Upload\";
        GalleryPhysicalPath = WebsiteRootPhysicalDir + @"PhotoGallery\";
    }

    void UploadS3Object(string bucket, string FileNameToUpload)
    {
        AmazonS3Config s3c = new AmazonS3Config();

        s3c.ServiceURL = PhotoGalleryBaseUrl;
        AmazonS3Client s3Client = new AmazonS3Client(s3c);
       
        PutObjectRequest putObject = new PutObjectRequest();
        putObject.BucketName = bucket;
       
        putObject.CannedACL = S3CannedACL.PublicRead;
        putObject.FilePath = UploadPhysicalPath + FileNameToUpload;
        s3Client.PutObject(putObject);
    }
    

    private DataTable DataTableFromRDS(DataTable dt)
    {
        string cnstr = "server=";
        cnstr += dbinstance + ";User Id=";
        cnstr += userid + ";password=";
        cnstr += password + ";database=";
        cnstr += database;

        // create the database query
        string query = "SELECT * FROM images";

        MySqlConnection conn = new MySqlConnection(cnstr);
    
        MySqlDataAdapter dAdapter = new MySqlDataAdapter(query, conn);
        MySqlCommandBuilder cBuilder = new MySqlCommandBuilder(dAdapter);
        dAdapter.Fill(dt);
        return dt;
    }

}
