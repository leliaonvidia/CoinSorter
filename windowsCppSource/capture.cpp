#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>
#include <opencv2/imgproc/imgproc.hpp>
#include <iosfwd>
#include <memory>
#include <string>
#include <utility>
#include <vector>
#include <windows.h>
#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
using std::string;

///for web cam
#include <iostream>
using namespace cv;
using std::cout;
using std::endl;
///for web cam

extern "C" __declspec(dllexport) int captureFromWebCam(int imageID, int showImages);
void deskew(cv::Mat& src, cv::Mat& dst);
Point CoinCenter(Mat input, int showImages);
Mat CropToCenter(Mat input, Point coinCenter);

int captureFromWebCam(int imageID, int showImages)
{
	//cout << "01" << endl;
	static bool setup = false;
	double dWidth, dHeight;
	static VideoCapture cap;
	//cout << "02" << endl;
	if (setup == false) {

		cap.open(0); // open the video camera no. 0

		if (!cap.isOpened())  // if not success, exit program
		{
			cout << "Cannot open the video cam" << endl;
			return -1;
		}

		dWidth = cap.get(CV_CAP_PROP_FRAME_WIDTH); //get the width of frames of the video
		dHeight = cap.get(CV_CAP_PROP_FRAME_HEIGHT); //get the height of frames of the video

		cout << "Frame size : " << dWidth << " x " << dHeight << endl;

		if (showImages == 1){
			namedWindow("MyVideo", CV_WINDOW_AUTOSIZE); //create a window called "MyVideo"
		}
		//cap.set(CV_CAP_PROP_BRIGHTNESS, 0);
		//cap.set(CV_CAP_PROP_CONTRAST, 47);
		//cap.set(CV_CAP_PROP_SATURATION, 32);
		//cap.set(CV_CAP_PROP_GAIN, 24);

		setup = true;
	}
	
	Mat frame;
	bool bSuccess;
	bSuccess = cap.read(frame); // read a new frame from video
	waitKey(1);
	//for whatever reasons it's keeping a frame in a buffer, or something, and you have to call read twice to get a new frame:
	bSuccess = cap.read(frame); // read a new frame from video

	if (!bSuccess) //if not success, break loop
	{
		cout << "Cannot read a frame from video stream" << endl;
		return 0;
	}
	//cout << "03" << endl;
	cv::Mat deskewedFrame = Mat::zeros(frame.rows, frame.cols, frame.type());
	//cout << "04" << endl;
	deskew(frame, deskewedFrame);
	//cout << "05" << endl;

	imwrite("F:/OpenCV/" + std::to_string(imageID) + "raw.jpg", deskewedFrame);
	Point coinCenter = CoinCenter(deskewedFrame, showImages);
	//cout << "06" << endl;
	
	if (coinCenter.x == 0) {
		cout << "Coin Not found" << endl;
		return 0;
	}
	
	cv::Mat crop = CropToCenter(deskewedFrame, coinCenter);
	imwrite("F:/OpenCV/" + std::to_string(imageID) + ".jpg", crop);

	if (showImages == 1){
		imshow("MyVideo", crop);

		if (waitKey(1) == 27) //wait for 'esc' key press for 30ms. If 'esc' key is pressed, break loop
		{
			cout << "esc key is pressed by user" << endl;
			destroyWindow("MyVideo");
			return 0;
		}
	}

	return 0;
}

