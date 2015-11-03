﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ImageClassifier
{
    public partial class frmLabel : Form
    {
        List<String> fileList = new List<String>();
        int LastListBoxWorkingLabelSelectedIndex = -1;
        public frmLabel()
        {
            InitializeComponent();
        }


        private String getMainImageDirectory()
        {
            String mainImageDirectory = txtMainImageDirectory.Text;
            if (!Directory.Exists(mainImageDirectory))
            {
                txtMainImageDirectory.Focus();
                MessageBox.Show("Directory Does Not Exist");
                return null;
            }
            return mainImageDirectory;
        }

        private void cmdReadDirectoryAndRefresh_Click(object sender, EventArgs e)
        {

            if (getMainImageDirectory() == null)
            {
                return;
            }


            listBoxWorkingLabel.Items.Clear();
            listBoxClickList.Items.Clear();
            listBoxLabelAllShown.Items.Clear();

            listBoxWorkingLabel.Items.Add("No Label");
            String[] directorys = Directory.GetDirectories(getMainImageDirectory());
            foreach (String dir in directorys)
            {
                String dirName = dir.Replace(getMainImageDirectory() + "\\", "");
                listBoxWorkingLabel.Items.Add(dirName);
            }
            listBoxClickList.Items.AddRange(listBoxWorkingLabel.Items);
            listBoxClickList.Items.Add("Delete");
            listBoxLabelAllShown.Items.AddRange(listBoxWorkingLabel.Items);
            listBoxLabelAllShown.Items.Add("Delete");

            listBoxWorkingLabel.SelectedIndex = 0;
            listBoxClickList.SelectedIndex = 1;
            listBoxLabelAllShown.SelectedIndex = 1;

            ImageRefresh(true);
        }


        private void ImageRefresh(bool newFileList)
        {
            String workingDirectory = getMainImageDirectory();
            if (workingDirectory == null)
            {
                return;
            }

            if (listBoxWorkingLabel.SelectedIndex != 0)
            {
                workingDirectory = workingDirectory + "//" + listBoxWorkingLabel.SelectedItem;
            }

            if (newFileList)
            {
                fileList.Clear();
                String[] files = Directory.GetFiles(workingDirectory);
                fileList.AddRange(files);
            }
            
            
            List<int> MislabeledImageIDs = ImagesDB.GetMislabeledImageIDs();
            List<String> weakFileList = new List<String>();
            foreach (String fileName in fileList)
            {
                int imageID = Convert.ToInt32(fileName.Substring(fileName.Length - 12, 8)) - 10000000;
                if (MislabeledImageIDs.Contains(imageID + 10000000))
                {
                    weakFileList.Add(fileName);
                }
            }
            fileList = weakFileList;

            groupBoxImages.Controls.Clear();
            int imageSize = 120;
            for (int y = 0; y < 800; y = y + imageSize + 10)
            {
                for (int x = 0; x < 800; x = x + imageSize + 10)
                {
                    if (fileList.Count == 0)
                    {
                        return;
                    }
                    String fileName = fileList[0];
                    int imageID = Convert.ToInt32(fileName.Substring(fileName.Length - 12, 8)) - 10000000;
                    fileList.RemoveAt(0);
                    PictureBox pictureBox = new PictureBox();
                    pictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
                    pictureBox.Tag = fileName;
                    pictureBox.BackgroundImage = (Image)CloneImage(fileName);
                    pictureBox.Height = imageSize;
                    pictureBox.Width = imageSize;
                    pictureBox.Left = x;
                    pictureBox.Top = y + 10;
                    pictureBox.Click += new EventHandler(pictureBox_Click);
                    pictureBox.Paint += new PaintEventHandler((sender, e) =>
                    {
                        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                        e.Graphics.DrawString(imageID.ToString(), Font, Brushes.Blue, 3, 3);
                    });
                    groupBoxImages.Controls.Add(pictureBox);


                }
            }
        }


        private Bitmap CloneImage(string aImagePath)
        {
            // create original image
            Image originalImage = new Bitmap(aImagePath);

            // create an empty clone of the same size of original
            Bitmap clone = new Bitmap(originalImage.Width, originalImage.Height);

            // get the object representing clone's currently empty drawing surface
            Graphics g = Graphics.FromImage(clone);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

            // copy the original image onto this surface
            g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);

            // free graphics and original image
            g.Dispose();
            originalImage.Dispose();

            return clone;
        }

        private void HandlePictureBoxClick(PictureBox pictureBox, bool allShown, bool skipCommand)
        {
            String imageName = pictureBox.Tag.ToString();
            String workingDirectory = getMainImageDirectory();
            if (workingDirectory == null)
            {
                return;
            }

            groupBoxImages.Controls.Remove(pictureBox);
            if (skipCommand)
            {
                return;
            }


            String selectedCommand;
            if (allShown)
            {
                selectedCommand = listBoxLabelAllShown.SelectedItem.ToString();
            }
            else
            {
                selectedCommand = listBoxClickList.SelectedItem.ToString();
            }

            if (selectedCommand == "Delete")
            {
                File.Delete(imageName);
            }
            else
            {
                String newDirectory = workingDirectory + "\\";
                if (listBoxClickList.SelectedIndex != 0)
                {
                    newDirectory += selectedCommand + "\\";
                }
                FileInfo fi = new FileInfo(imageName);
                File.Move(imageName, newDirectory + fi.Name);
            }
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            PictureBox pictureBox = (PictureBox)sender;
            HandlePictureBoxClick(pictureBox, false, false);
        }

        private void listBoxWorkingLabel_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBoxLabelAllShown.SelectedIndex = listBoxWorkingLabel.SelectedIndex;
            if (listBoxWorkingLabel.SelectedIndex == LastListBoxWorkingLabelSelectedIndex)
            {
                OnlyGetMore();
            }
            else
            {
                ImageRefresh(true);
            }
            LastListBoxWorkingLabelSelectedIndex = listBoxWorkingLabel.SelectedIndex;
        }


        private void cmdLabelAll_Click(object sender, EventArgs e)
        {
            HandleAll(false);
        }

        private void HandleAll(bool skipCommand)
        {
            //A new list is needed because HandlePictureBoxClick is removing items from groupBoxImages.Controls. 
            List<PictureBox> pictureBoxes = new List<PictureBox>();

            foreach (PictureBox pictureBox in groupBoxImages.Controls)
            {
                pictureBoxes.Add(pictureBox);
            }

            foreach (PictureBox pictureBox in pictureBoxes)
            {
                HandlePictureBoxClick(pictureBox, true, skipCommand);
            }
            ImageRefresh(false);
        }

        private void cmdGetMore_Click(object sender, EventArgs e)
        {
            OnlyGetMore();
        }

        private void OnlyGetMore()
        {
            if (fileList.Count == 0)
            {
                ImageRefresh(true);
            }
            else
            {
                HandleAll(true);
            }
        }

    }
}
