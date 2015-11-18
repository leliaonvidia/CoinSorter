﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Data.SQLite;

namespace ImageClassifier
{
    class ImagesDB : SQLiteDB
    {
        [DllImport("E:\\build\\Caffe-prefix\\src\\Caffe-build\\examples\\cpp_classification\\Debug\\classification-d.dll")]
        private static extern int Augment(String fileDir, String augmentDirectory, int imageID, float angle);
        [DllImport("E:\\build\\Caffe-prefix\\src\\Caffe-build\\examples\\cpp_classification\\Debug\\classification-d.dll")]
        private static extern int CropForDate(String fileDir, String dateCropDirectory, int imageID, float angle, bool augment);

        static public List<int> GetMislabeledImageIDs()
        {
            List<int> mislabeledImageIDs = new List<int>();
            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("Select Images.ImageID");
            SQL.AppendLine("From Images");
            SQL.AppendLine("Inner Join Results");
            SQL.AppendLine("On Images.ImageID = Results.ImageID");
            SQL.AppendLine("And Results.Score < .999");
            SQL.AppendLine("And Images.DesignID = Results.DesignID");
            SQL.AppendLine("Order by 1;");
            Open();
            SQLiteDataReader reader = GetNewReader(SQL.ToString());
            while (reader.Read())
            {
                mislabeledImageIDs.Add(reader.GetInt32(0));
            }
            reader.Close();
            Close();
            return mislabeledImageIDs;
        }

        static public List<int> GetDateUnLabeledImageIDs(int top, int Date)
        {
            List<int> mislabeledImageIDs = new List<int>();
            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("Select Images.ImageID");
            SQL.AppendLine("From Images");
            SQL.AppendLine("Where DateGT is null");
            SQL.AppendLine("and Centered = 1");
            SQL.AppendLine("and DesignID = 1");
            SQL.AppendLine("And Date=" + Date);
            SQL.AppendLine("Order by DateResult Desc;");
            Open();
            SQLiteDataReader reader = GetNewReader(SQL.ToString());

            for (int x = 0; x < top; x++)
            {
                if (reader.Read()){
                    mislabeledImageIDs.Add(reader.GetInt32(0));
                }
                else
                {
                    break;
                }

            }
            reader.Close();
            Close();
            return mislabeledImageIDs;
        }

        static public void AddImage(int imageID, int DesignID)
        {

            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("BEGIN;");
            SQL.Append("Insert into Images (ImageID,DesignID) values (");
            SQL.Append(imageID + ",");
            SQL.AppendLine(DesignID + ");");
            SQL.AppendLine("COMMIT;");
            ExecuteQuery(SQL.ToString());
        }

        static public void AddImages(Dictionary<int, int> images)
        {
            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("BEGIN;");

            foreach (KeyValuePair<int, int> img in images)
            {
                SQL.Append("Insert into Images (ImageID,DesignID) values (");
                SQL.Append(img.Key + ",");
                SQL.AppendLine(img.Value + ");");
            }

            SQL.AppendLine("COMMIT;");
            Open();
            ExecuteQuery(SQL.ToString());
            Close();
        }

        static public void AddImages(List<ImageResult> imageResults)
        {
            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("BEGIN;");

            foreach (ImageResult imageResult in imageResults)
            {
                SQL.Append(GetInsertSQL(imageResult));
            }
            SQL.AppendLine("COMMIT;");
            Open();
            ExecuteQuery(SQL.ToString());
            Close();
        }

        static public String GetInsertSQL(ImageResult imageResult)
        {
            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("Insert into Images (ImageID,Centered,CenterResult,DesignID,DesignResult,Angle,AngleResult,Date,DateResult) values (");
            SQL.Append(imageResult.ImageID + ",");
            SQL.Append(imageResult.Centered + ",");
            SQL.Append(imageResult.CenterResult + ",");
            SQL.Append(imageResult.DesignID + ",");
            SQL.Append(imageResult.DesignResult + ",");
            SQL.Append(imageResult.Angle + ",");
            SQL.Append(imageResult.AngleResult + ",");
            SQL.Append(imageResult.Date + ",");
            SQL.AppendLine(imageResult.DateResult + ");");
            return SQL.ToString();
        }

        static public void UpdateAngle(int imageID, float angle)
        {
            Open();
            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("BEGIN;");
            SQL.AppendLine("Update Images");
            SQL.AppendLine("Set angleGT = " + angle);
            SQL.AppendLine("Where ImageID = " + imageID + ";");
            SQL.AppendLine("COMMIT;");
            ExecuteQuery(SQL.ToString());
            Close();
        }

        static public void UpdateDate(int imageID, int date)
        {
            Open();
            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("BEGIN;");
            SQL.AppendLine("Update Images");
            SQL.AppendLine("Set dateGT = " + date);
            SQL.AppendLine("Where ImageID = " + imageID + ";");
            SQL.AppendLine("COMMIT;");
            ExecuteQuery(SQL.ToString());
            Close();
        }

        static public Dictionary<int, float> GetImageAngles()
        {
            Dictionary<int, float> imageAngles = new Dictionary<int, float>();
            StringBuilder SQL = new StringBuilder();
            SQL.AppendLine("Select Images.ImageID");
            SQL.AppendLine(", angle");
            SQL.AppendLine("From Images");
            SQL.AppendLine("Where angle is not null;");
            Open();
            SQLiteDataReader reader = GetNewReader(SQL.ToString());
            while (reader.Read())
            {
                imageAngles.Add(reader.GetInt32(0), reader.GetFloat(1));
            }
            reader.Close();
            Close();
            return imageAngles;
        }

        static public int GetImageIDFromFileName(String fileName)
        {
            if (fileName.Contains("raw"))
            {
                return Convert.ToInt32(fileName.Substring(0, 8)) - 10000000;
            }
            return Convert.ToInt32(fileName.Substring(fileName.Length - 12, 8)) - 10000000;
        }


        static public void AugmentImages(String directory, int numberOfRotations)
        {
            String cropDirectory = directory + "/Heads/";
            String augmentDirectory = directory + "/HeadsWithRotation/";
            if (!Directory.Exists(augmentDirectory))
            {
                Directory.CreateDirectory(augmentDirectory);
            }

            for (int a = 0; a < 360; a++)
            {
                if (!Directory.Exists(augmentDirectory + a))
                {
                    Directory.CreateDirectory(augmentDirectory + a.ToString().PadLeft(3, '0'));
                }
            }

            Dictionary<int, float> imageAngles = GetImageAngles();

            foreach (KeyValuePair<int, float> imageAngle in imageAngles)
            {
                String fileName = cropDirectory + imageAngle.Key + ".jpg";
                Augment(cropDirectory, augmentDirectory, imageAngle.Key, imageAngle.Value);
            }
        }

        //static public void AugmentImages(String directory)
        //{
        //    String cropDirectory = directory + "/Crops/";
        //    String augmentDirectory = directory + "/Augmented/";
        //    if (!Directory.Exists(augmentDirectory))
        //    {
        //        Directory.CreateDirectory(augmentDirectory);
        //    }

        //    String[] files;
        //    files = Directory.GetFiles(cropDirectory, "*.*", SearchOption.AllDirectories);
        //    foreach (string image_file in files)
        //    {
        //        if (image_file.Contains("bad"))
        //        {
        //            continue;
        //        }
        //        //int imageID = Convert.ToInt32(image_file.Substring(image_file.Length - 12, 8));
        //        for (int angle = 13; angle < 360; angle = angle + 21)
        //        {
        //            String fileName = image_file.Replace("Crops/", "Augmented/");
        //            fileName = fileName.Replace(".jpg", angle.ToString().PadLeft(3, '0') + ".png");
        //            Augment(image_file, fileName, angle);
        //        }

        //    }
        //}

        static public void CropForDates(String directory, bool augment)
        {
            String cropDirectory = directory + "/Heads/";
            String dateCropDirectory = directory + "/Dates/";
            String dateCropDirectoryAugmented = directory + "/DatesAugmented/";

            if (!Directory.Exists(dateCropDirectory))
            {
                Directory.CreateDirectory(dateCropDirectory);
            }

            Dictionary<int, float> imageAngles = GetImageAngles();

            if (augment)
            {
                foreach (string fileName in Directory.GetFiles(dateCropDirectory, "*.*", SearchOption.AllDirectories))
                {
                    int imageID;
                    int.TryParse(fileName.Substring(fileName.Length - 12, 8), out imageID);
                    if (imageAngles.Keys.Contains(imageID))
                    {
                        String newDateCropDirectory = fileName.Substring(0, fileName.Length - 12);
                        newDateCropDirectory = newDateCropDirectory.Replace("Dates", "DatesAugmented");
                        CropForDate(cropDirectory, newDateCropDirectory, imageID, imageAngles[imageID], false);
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<int, float> imageAngle in imageAngles)
                {
                    String fileName = cropDirectory + imageAngle.Key + ".jpg";
                    //CropForDate(cropDirectory, dateCropDirectory, imageAngle.Key, imageAngle.Value, augment);
                }
            }
        }
    }
}
