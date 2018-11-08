#pragma once
#include "stdafx.h"
#include <iostream>
#include <stdio.h>
#include <stdlib.h>
#include <conio.h>
#include <fstream>
#include <sstream>
#include <string>
#include <opencv2/opencv.hpp>
#include <opencv2/core/utility.hpp>
#include <opencv2/tracking.hpp>
#include <opencv2/tracking/tracker.hpp>
#include <opencv2/videoio.hpp>
#include <msclr/marshal_cppstd.h> // .NET string to STL string
#using <system.dll>
using namespace System;
using namespace System::ComponentModel;
using namespace System::Collections;
using namespace System::Windows::Forms;
using namespace System::Data;
using namespace System::Drawing;
using namespace System::Diagnostics;
using namespace msclr::interop;
using namespace cv;

struct AOI {
	int L; int R; int T; int D;	//left, right, top, down
	int Area;	//AOI area
	bool confirm; bool mod;	//"confirm" present which AOIs need to be tracked; "mod" present which AOIs are modified by user
	int frame;	//time frame
};

namespace AOI_Tool {

	public ref class Program {

	public:
		Program() {
		}
		~Program() {
		}
		std::string static StringConverter(System::String^);

		//bool OverLapping(AOI, AOI);
		//bool Adjent(AOI, AOI);
		//int DfsVisit(int, int, int); 
		//int SceneChange(Mat, Mat, int);
		//bool IsRed(Scalar);
		//void ResetAOI(AOI&);
		//void PrintAOIinfo(AOI);
		//void onMouse(int, int, int, int, void*);
		//void PrintFixation(Mat, int);

		//bool inAOI(Point2f, AOI);

		void Mosaic(Mat, Mat&);
		void Scan(Mat, Mat&);
		void FindAOI(Mat, Mat&, Mat&);
		void SortAOI(Mat&, int);
		void ReDraw(Mat&, int);
		int LoadFixation(System::String^);
		int MainFunction(System::String^, int, int, int, int, int, int);
	};
}
