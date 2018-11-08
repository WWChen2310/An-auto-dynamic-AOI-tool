#include "stdafx.h"
#include "Mainprocess.h"

using namespace std;

int MOSAIC_SIZE;
int AREA_MIN;
int COLOR_DIFF;
int BANNER_HEIGHT;
int TOOLBAR_HEIGHT;
int Print_FLG;
int SCENECHANGE_THRESHOLD;
int DIST;
int AOI_MAXNUM;

Mat src;
Mat src_gray;
Mat previous;
Mat mosaic;
Mat scan; //Mat scan2;
Mat AOI_color;
Mat AOI_show;

Mat tmp1;
Mat tmp2;
Mat tmp3;

vector<double> Fixation_matrix;
int frame_count = 0;
int FLG_pause = 0;
int FLG_changescene = 0;
int AOI_count = 0;
float Total_Acc = 0;
float Total_count = 0;
int AOI_correct = 0;
int AOI_total = 0;
vector<double> area_c;
vector<Point2f> center_c;

int **AOImap;
int **FLGmap;
AOI AOIarray[1000];

Point2f eyefixation;
Point2f vertexLU;
Point2f vertexRD;

string Frame;
string AOIs;

vector<string> trackerTypes = { "BOOSTING", "MIL", "KCF", "TLD", "MEDIANFLOW", "GOTURN", "MOSSE", "CSRT" };
MultiTracker multiTracker;
vector<Rect2d> objects;
vector<Rect> ROIs;
vector<Ptr<Tracker> > algorithms;

string AOI_Tool::Program::StringConverter(System::String^ nets) {
	marshal_context^ context = gcnew marshal_context();
	string stds = context->marshal_as<std::string>(nets);
	System::String^ s = gcnew System::String(stds.c_str());
	Debug::WriteLine("change {} to string", s);
	delete context;
	return stds;
}
void onMouse(int Event, int x, int y, int flags, void* param) {
	Point2f p;
	p.x = x; p.y = y;
	if (FLG_pause == 1) {
		//func1 create new AOI
		if (Event == EVENT_LBUTTONDOWN) {
			vertexLU.x = x;
			vertexLU.y = y;
			Debug::WriteLine("left click down");
			//cout << "p: " << p.x << " " << p.y << endl;

		}
		if (Event == EVENT_LBUTTONUP) {
			vertexRD.x = x;
			vertexRD.y = y;
			//cout << "left click up" << endl;
			if (vertexLU.x != vertexRD.x && vertexLU.y != vertexRD.y) {
				if (vertexLU.x > vertexRD.x && vertexLU.y > vertexRD.y) {
					int tx = vertexLU.x;
					int ty = vertexRD.y;
					vertexLU.x = vertexRD.x;
					vertexLU.y = vertexRD.y;
					vertexRD.x = tx;
					vertexRD.y = ty;
				}
				rectangle(AOI_show, cv::Point(vertexLU), cv::Point(vertexRD), Scalar(0, 0, 255), 3);
				rectangle(tmp2, cv::Point(vertexLU), cv::Point(vertexRD), Scalar(0, 0, 200), FILLED);
				//write into AOIarray
				AOIarray[AOI_count].L = vertexLU.x / MOSAIC_SIZE;
				AOIarray[AOI_count].T = vertexLU.y / MOSAIC_SIZE;
				AOIarray[AOI_count].R = vertexRD.x / MOSAIC_SIZE;
				AOIarray[AOI_count].D = vertexRD.y / MOSAIC_SIZE;;
				AOIarray[AOI_count].mod = true;
				AOIarray[AOI_count].confirm = true;
				AOIarray[AOI_count].frame = frame_count;
				AOI_count++;
			}
			

			imshow("AOI_show", AOI_show);
		}
		//func2 comfirm AOI
		if (Event == EVENT_RBUTTONDOWN) {
			int in = -1;
			for (int i = 0; i < AOI_count; i++) {
				if (AOIarray[i].L * MOSAIC_SIZE <= x && (AOIarray[i].R + 1) * MOSAIC_SIZE >= x && AOIarray[i].T * MOSAIC_SIZE <= y && (AOIarray[i].D + 1) * MOSAIC_SIZE >= y)
					in = i;
			}
			if (in != -1) {
				if (AOIarray[in].confirm == false) {
					rectangle(AOI_show, cv::Point(AOIarray[in].L * MOSAIC_SIZE, AOIarray[in].T * MOSAIC_SIZE), cv::Point((AOIarray[in].R + 1) * MOSAIC_SIZE, (AOIarray[in].D + 1) * MOSAIC_SIZE), Scalar(0, 0, 255), 3);
					AOIarray[in].confirm = true;
				}
				else {
					rectangle(AOI_show, cv::Point(AOIarray[in].L * MOSAIC_SIZE, AOIarray[in].T * MOSAIC_SIZE), cv::Point((AOIarray[in].R + 1) * MOSAIC_SIZE, (AOIarray[in].D + 1) * MOSAIC_SIZE), Scalar(0, 255, 0), 3);
					AOIarray[in].confirm = false;
				}

			}
			imshow("AOI_show", AOI_show);
		}
	}
}
void CalculateAcurrancy() {
	for (int i = 1; i < AOI_count; i++) {
		if (AOIarray[i].confirm == true && AOIarray[i].mod == false) {
			AOI_total++;
			AOI_correct++;
			Debug::WriteLine("true {0} framecount {1} {2} {3} {4} {5}", i, frame_count, (AOIarray[i].L - 1) * MOSAIC_SIZE, (AOIarray[i].T - 1) * MOSAIC_SIZE, (AOIarray[i].R - AOIarray[i].L + 1) * MOSAIC_SIZE, (AOIarray[i].D - AOIarray[i].T + 1) * MOSAIC_SIZE);

		}
		else if (AOIarray[i].confirm == true && AOIarray[i].mod == true) {
			AOI_total++;
			Debug::WriteLine("mod {0} framecount {1} {2} {3} {4} {5}", i, frame_count, (AOIarray[i].L - 1) * MOSAIC_SIZE, (AOIarray[i].T - 1) * MOSAIC_SIZE, (AOIarray[i].R - AOIarray[i].L + 1) * MOSAIC_SIZE, (AOIarray[i].D - AOIarray[i].T + 1) * MOSAIC_SIZE);

		}
		if (AOIarray[i].confirm == false && AOIarray[i].R != -1) {
			AOI_total++;
			Debug::WriteLine("false {0} framecount {1} {2} {3} {4} {5}", i, frame_count, (AOIarray[i].L - 1) * MOSAIC_SIZE, (AOIarray[i].T - 1) * MOSAIC_SIZE, (AOIarray[i].R - AOIarray[i].L + 1) * MOSAIC_SIZE, (AOIarray[i].D - AOIarray[i].T + 1) * MOSAIC_SIZE);
		}
	}
}
bool OverLapping(AOI A1, AOI A2) {
	if (((A1.L <= A2.R && A1.L >= A2.L) ||
		(A1.R <= A2.R && A1.R >= A2.L)) &&
		((A1.T <= A2.D && A1.T >= A2.T) ||
		(A1.D <= A2.D && A1.D >= A2.T)))
		return true;
	else return false;
}
bool Adjent(AOI A1, AOI A2) {
	if (((A1.R + 1 == A2.L) && ((A2.D >= A1.T && A2.D <= A1.D) || (A2.T >= A1.T && A2.T <= A1.D))) ||
		((A1.D + 1 == A2.T) && ((A2.L >= A1.L && A2.L <= A1.R) || (A2.R >= A1.L && A2.R <= A1.R))) ||
		((A2.R + 1 == A1.L) && ((A1.D >= A2.T && A1.D <= A2.D) || (A1.T >= A2.T && A1.T <= A2.D))) ||
		((A2.D + 1 == A1.T) && ((A1.L >= A2.L && A1.L <= A2.R) || (A1.R >= A2.L && A1.R <= A2.R))))
		return true;
	else return false;
}
int DfsVisit(int m, int n, int AOI_ID) {
	if (FLGmap[m][n] == 1) return 0;
	else if (FLGmap[m][n] == 0) {
		FLGmap[m][n] = 1;
		AOImap[m][n] = AOI_ID;
		AOIarray[AOI_ID].Area++;
		if (FLGmap[m][n - 1] == 0)	//L
			DfsVisit(m, n - 1, AOI_ID);
		if (FLGmap[m][n + 1] == 0)	//R
			DfsVisit(m, n + 1, AOI_ID);
		if (FLGmap[m - 1][n] == 0)	//T
			DfsVisit(m - 1, n, AOI_ID);
		if (FLGmap[m + 1][n] == 0)	//D
			DfsVisit(m + 1, n, AOI_ID);
		//8 directions
		if (FLGmap[m - 1][n - 1] == 0)	//LT
			DfsVisit(m - 1, n - 1, AOI_ID);
		if (FLGmap[m + 1][n - 1] == 0)	//LD
			DfsVisit(m + 1, n - 1, AOI_ID);
		if (FLGmap[m - 1][n + 1] == 0)	//RT
			DfsVisit(m - 1, n + 1, AOI_ID);
		if (FLGmap[m + 1][n + 1] == 0)	//RD
			DfsVisit(m + 1, n + 1, AOI_ID);
	}
	return 0;
}
int SceneChange(Mat pre, Mat now, int SCENECHANGE_THRESHOLD) {
	Scalar c1, c2;
	int s = 0, c = 0;
	for (int i = 0; i < pre.rows; i++) {
		for (int j = 0; j < pre.cols; j++) {
			c1 = pre.at<Vec3b>(i, j);
			c2 = now.at<Vec3b>(i, j);
			s += pow(pow(c1.val[0] - c2.val[0], 2) + pow(c1.val[1] - c2.val[1], 2) + pow(c1.val[2] - c2.val[2], 2), 0.5);
			c++;
		}
	}
	float r = float(s) / c;
	cout << "scenechange value: " << r << endl;
	if (r > SCENECHANGE_THRESHOLD) return 1;
	else return 0;
}
bool IsRed(Scalar c) {
	if (c.val[0] == 0 && c.val[1] == 0 && c.val[2] == 255)
		return 1;
	else return 0;
}
void ResetAOI(AOI& clear) {
	clear.L = 0; clear.R = 0; clear.T = 0; clear.D = 0;
	clear.Area = 0;
	clear.confirm = false;
}
void PrintAOIinfo(AOI print) {
	cout << "AOI L" << print.L << " R" << print.R << " T" << print.T << " D" << print.D << endl;
	cout << "Area " << print.Area << endl;
}
void PrintFixation(Mat img, int FPS) {
	int interval = 0;
	//cout << "frame count : " << frame_count << endl;
	while (Fixation_matrix[interval * 3] * FPS / 100 < frame_count) interval++;	//find time interval number
																				//cout << "time interval number : " << interval << endl;
	eyefixation.x = Fixation_matrix[interval * 3 + 1];
	eyefixation.y = Fixation_matrix[interval * 3 + 2];
	cout << "point position " << eyefixation.x << " " << eyefixation.y << endl;
	circle(img, eyefixation, 20, Scalar(255, 0, 0), 1);
}
Ptr<Tracker> createTrackerByName(string trackerType)
{
	Ptr<Tracker> tracker;
	if (trackerType == trackerTypes[0])
		tracker = TrackerBoosting::create();
	else if (trackerType == trackerTypes[1])
		tracker = TrackerMIL::create();
	else if (trackerType == trackerTypes[2])
		tracker = TrackerKCF::create();
	else if (trackerType == trackerTypes[3])
		tracker = TrackerTLD::create();
	else if (trackerType == trackerTypes[4])
		tracker = TrackerMedianFlow::create();
	else if (trackerType == trackerTypes[5])
		tracker = TrackerGOTURN::create();
	else if (trackerType == trackerTypes[6])
		tracker = TrackerMOSSE::create();
	else if (trackerType == trackerTypes[7])
		tracker = TrackerCSRT::create();
	else {
		Debug::WriteLine("Tracker type error");
	}
	return tracker;
}// create tracker by name

// Process
void AOI_Tool::Program::Mosaic(Mat img, Mat& result) {	//mosaic
	previous = src.clone();
	//cout << src.cols << " " << src.cols / MOSAIC_SIZE << endl;	//1024
	//cout << src.rows << " " << src.rows / MOSAIC_SIZE << endl;	//768
	for (int m = 0; m * MOSAIC_SIZE < img.rows; m++) {
		for (int n = 0; n * MOSAIC_SIZE < img.cols; n++) {
			//for each block
			int B = 0;	int G = 0;	int R = 0; int pn = 0; Scalar color;
			for (int i = 0; i < MOSAIC_SIZE && i + m * MOSAIC_SIZE < img.rows; i++) {
				for (int j = 0; j < MOSAIC_SIZE && j + n * MOSAIC_SIZE < img.cols; j++) {
					color = img.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE);
					B += color.val[0];
					G += color.val[1];
					R += color.val[2];
					pn++;
					//cout <<"at " << i + m * MOSAIC_SIZE <<","<< j + n * MOSAIC_SIZE << "B " << blue << " G " << green << " R " << red << endl;
				}
			}
			//cout << "block " << m << "," << n << "B " << B << " G " << G << " R " << R  << " pn" << pn  << endl;
			B = B / pn;
			G = G / pn;
			R = R / pn;
			//cout << "block " << m << "," << n << "B " << B << " G " << G << " R " << R << " pn" << pn << endl;
			for (int i = 0; i < MOSAIC_SIZE && i + m * MOSAIC_SIZE < img.rows; i++) {
				for (int j = 0; j < MOSAIC_SIZE && j + n * MOSAIC_SIZE < img.cols; j++) {
					//cout << "point " << i + m * MOSAIC_SIZE << "," << j + n * MOSAIC_SIZE << "B " << B << " G " << G << " R " << R << endl;
					result.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[0] = B;
					result.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[1] = G;
					result.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[2] = R;
					//cout <<"at " << i + m * MOSAIC_SIZE <<","<< j + n * MOSAIC_SIZE << "B " << blue << " G " << green << " R " << red << endl;
				}
			}
		}
	}
	//imshow("mosaic", result);
}
void AOI_Tool::Program::Scan(Mat img, Mat& result) {
	Scalar c1, c2;

	for (int i = 0; i < img.rows; i++) {
		int flagj = 0;
		for (int j = 0; j < img.cols && j < img.cols - 1; j++) {
			c1 = img.at<Vec3b>(i, j);
			c2 = img.at<Vec3b>(i, j + 1);
			if (pow(pow(c1.val[0] - c2.val[0], 2) + pow(c1.val[1] - c2.val[1], 2) + pow(c1.val[2] - c2.val[2], 2), 0.5) <= COLOR_DIFF) {
				flagj++;
			}
			else if (flagj >= DIST) {
				//cout << "find edge " << j << "," << i << endl;
				result.at<Vec3b>(i, j).val[0] = 0;
				result.at<Vec3b>(i, j).val[1] = 0;
				result.at<Vec3b>(i, j).val[2] = 255;
				flagj = 0;
			}
		}
	}
	for (int j = 0; j < img.cols; j++) {
		int flagi = 0;
		for (int i = 0; i < img.rows && i < img.rows - 1; i++) {
			c1 = img.at<Vec3b>(i, j);
			c2 = img.at<Vec3b>(i + 1, j);
			if (pow(3 * pow(c1[0] - c2[0], 2) + 4 * pow(c1[1] - c2[1], 2) + 2 * pow(c1[2] - c2[2], 2), 0.5) <= COLOR_DIFF) {
				flagi++;
			}
			else if (flagi >= DIST) {
				result.at<Vec3b>(i, j).val[0] = 0;
				result.at<Vec3b>(i, j).val[1] = 0;
				result.at<Vec3b>(i, j).val[2] = 255;
				flagi = 0;
			}
		}
	}
	//imshow("scan", result);
}
void AOI_Tool::Program::FindAOI(Mat img, Mat& AOI_color, Mat& result) {
	//initialize
	for (int m = 0; m * MOSAIC_SIZE < img.rows; m++) {
		for (int n = 0; n * MOSAIC_SIZE < img.cols; n++) {
			AOImap[m][n] = -1;
			FLGmap[m][n] = -1;
		}
	}
	for (int i = 0; i < AOI_MAXNUM; i++) ResetAOI(AOIarray[i]);

	//part1 在AOI_color上劃出區域
	for (int m = (BANNER_HEIGHT % MOSAIC_SIZE == 0) ? BANNER_HEIGHT / MOSAIC_SIZE + 1 : BANNER_HEIGHT / MOSAIC_SIZE; (m + 1) * MOSAIC_SIZE < img.rows - TOOLBAR_HEIGHT; m++) {	//去掉上下
		for (int n = 1; (n + 1) * MOSAIC_SIZE < img.cols; n++) {	//第一個&最後一個不算 從第二個開始
																	//for each block
			Scalar c1 = img.at<Vec3b>(5 + m * MOSAIC_SIZE, 9 + (n - 1) * MOSAIC_SIZE);//L
			Scalar c2 = img.at<Vec3b>(5 + m * MOSAIC_SIZE, 9 + n * MOSAIC_SIZE);//R
			Scalar c3 = img.at<Vec3b>(9 + (m - 1) * MOSAIC_SIZE, 5 + n * MOSAIC_SIZE);//U
			Scalar c4 = img.at<Vec3b>(9 + m * MOSAIC_SIZE, 5 + n * MOSAIC_SIZE);//D
			if ((IsRed(c1) && IsRed(c2) && IsRed(c3)) || (IsRed(c1) && IsRed(c2) && IsRed(c4)) || (IsRed(c1) && IsRed(c3) && IsRed(c4)) || (IsRed(c2) && IsRed(c3) && IsRed(c4))) {
				//整個塗紅
				//AOImap[m][n] = AOI_count;
				FLGmap[m][n] = 0;
				for (int i = 0; i < MOSAIC_SIZE && i + (m + 1) * MOSAIC_SIZE < img.rows; i++) {
					for (int j = 0; j < MOSAIC_SIZE && j + (n + 1) * MOSAIC_SIZE < img.cols; j++) {
						//cout << "point " << i + m * MOSAIC_SIZE << "," << j + n * MOSAIC_SIZE << "B " << B << " G " << G << " R " << R << endl;
						AOI_color.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[0] = 0;
						AOI_color.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[1] = 0;
						AOI_color.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[2] = 255;
						//cout <<"at " << i + m * MOSAIC_SIZE <<","<< j + n * MOSAIC_SIZE << "B " << blue << " G " << green << " R " << red << endl;
					}
				}
			}

		}
	}

	//imshow("AOI_colored", AOI_color);
	//part2
	AOI_count = 0;

	for (int m = (BANNER_HEIGHT % MOSAIC_SIZE == 0) ? BANNER_HEIGHT / MOSAIC_SIZE + 1 : BANNER_HEIGHT / MOSAIC_SIZE; (m + 1) * MOSAIC_SIZE < img.rows - TOOLBAR_HEIGHT; m++) {	//第一個&最後一個不算 從第二個開始
		for (int n = 1; (n + 1) * MOSAIC_SIZE < img.cols; n++) {
			//flag_start = (FLGmap[m][n] == 0) ? 1 : 0;
			if (FLGmap[m][n] == 0) {
				AOI_count++;
				DfsVisit(m, n, AOI_count);
			}
		}
	}

	//part3 show the result
	//Print_fixation(AOI_show);
	for (int i = 1; i <= AOI_count; i++) {	//find L,R,T,D
											//int L = 0; int R = 0; int T = 0; int D = 0;
		for (int m = (BANNER_HEIGHT % MOSAIC_SIZE == 0) ? BANNER_HEIGHT / MOSAIC_SIZE + 1 : BANNER_HEIGHT / MOSAIC_SIZE; (m + 1) * MOSAIC_SIZE < img.rows - TOOLBAR_HEIGHT; m++) {	//第一個&最後一個不算
			for (int n = 1; (n + 1) * MOSAIC_SIZE < img.cols; n++) {
				if (FLGmap[m][n] == 1 && AOImap[m][n] == i) {
					AOIarray[i].L = (AOIarray[i].L == 0) ? n : (n < AOIarray[i].L) ? n : AOIarray[i].L;
					AOIarray[i].R = (AOIarray[i].R == 0) ? n : (n > AOIarray[i].R) ? n : AOIarray[i].R;
					AOIarray[i].T = (AOIarray[i].T == 0) ? m : (m < AOIarray[i].T) ? m : AOIarray[i].T;
					AOIarray[i].D = (AOIarray[i].D == 0) ? m : (m > AOIarray[i].D) ? m : AOIarray[i].D;
				}
			}
		}

	}

	//imshow("AOI_show", result);
}
void AOI_Tool::Program::SortAOI(Mat& AOI_show, int times) {
	for (int n = 0; n < times; n++) {
		for (int i = 0; i < AOI_count; i++) {
			for (int j = 0; j < AOI_count; j++) {
				if (OverLapping(AOIarray[i], AOIarray[j]) || Adjent(AOIarray[i], AOIarray[j])) {
					AOIarray[i].R = AOIarray[i].R > AOIarray[j].R ? AOIarray[i].R : AOIarray[j].R;
					AOIarray[i].L = AOIarray[i].L < AOIarray[j].L ? AOIarray[i].L : AOIarray[j].L;
					AOIarray[i].T = AOIarray[i].T < AOIarray[j].T ? AOIarray[i].T : AOIarray[j].T;
					AOIarray[i].D = AOIarray[i].D > AOIarray[j].D ? AOIarray[i].D : AOIarray[j].D;
					AOIarray[i].confirm = false;
					AOIarray[i].mod = false;
					AOIarray[i].frame = frame_count;
					//AOIarray[i].Area = AOIarray[i].Area + AOIarray[j].Area;
					//AOIarray[j].Area = AOIarray[i].Area + AOIarray[j].Area;
				}
			}
		}
	}
	for (int i = 0; i < AOI_count; i++) {
		for (int j = i + 1; j < AOI_count; j++) {
			if (AOIarray[i].R == AOIarray[j].R && AOIarray[i].L == AOIarray[j].L && AOIarray[i].T == AOIarray[j].T && AOIarray[i].D == AOIarray[j].D) {
				AOIarray[j].R = -1;
				AOIarray[j].L = -1;
				AOIarray[j].T = -1;
				AOIarray[j].D = -1;
			}
		}
	}
	//imshow("AOI_show", AOI_show);
}
void AOI_Tool::Program::ReDraw(Mat& result, int print_flg) {
	//read from AOI array and draw AOI_show
	for (int i = 0; i < AOI_count; i++) {
		int flag_in = 0;
		if (print_flg == 1 && AOIarray[i].L * MOSAIC_SIZE < eyefixation.x && (AOIarray[i].R + 1) * MOSAIC_SIZE > eyefixation.x && AOIarray[i].T * MOSAIC_SIZE < eyefixation.y && (AOIarray[i].D + 1) * MOSAIC_SIZE > eyefixation.y) flag_in = 1;
		if (AOIarray[i].Area >= AREA_MIN) {
			if (flag_in) {
				rectangle(result, cv::Point(AOIarray[i].L * MOSAIC_SIZE, AOIarray[i].T * MOSAIC_SIZE), cv::Point((AOIarray[i].R + 1) * MOSAIC_SIZE, (AOIarray[i].D + 1) * MOSAIC_SIZE), Scalar(255, 0, 0), 3);
				//rectangle(tmp1, cv::Point(AOIarray[i].L * MOSAIC_SIZE, AOIarray[i].T * MOSAIC_SIZE), cv::Point((AOIarray[i].R + 1) * MOSAIC_SIZE, (AOIarray[i].D + 1) * MOSAIC_SIZE), Scalar(255, 0, 0), CV_FILLED);
			}
			else {
				rectangle(result, cv::Point(AOIarray[i].L * MOSAIC_SIZE, AOIarray[i].T * MOSAIC_SIZE), cv::Point((AOIarray[i].R + 1) * MOSAIC_SIZE, (AOIarray[i].D + 1) * MOSAIC_SIZE), Scalar(0, 255, 0), 3);
				//rectangle(tmp1, cv::Point(AOIarray[i].L * MOSAIC_SIZE, AOIarray[i].T * MOSAIC_SIZE), cv::Point((AOIarray[i].R + 1) * MOSAIC_SIZE, (AOIarray[i].D + 1) * MOSAIC_SIZE), Scalar(0, 0, 255), CV_FILLED);
			}
		}
	}
}
void TrackingSet(std::string trackertype) {	//set tracking objects
	//selectROIs("tracker", AOI_show, ROIs);
	for (int i = 1; i < AOI_count; i++) {
		if (AOIarray[i].confirm == true) {
			Rect R((AOIarray[i].L - 1) * MOSAIC_SIZE, (AOIarray[i].T - 1) * MOSAIC_SIZE, (AOIarray[i].R - AOIarray[i].L + 1) * MOSAIC_SIZE, (AOIarray[i].D - AOIarray[i].T + 1) * MOSAIC_SIZE);
			ROIs.push_back(R);
		}
	}
	Debug::WriteLine("AOI_confirm: {0}\n", ROIs.size());
	for (size_t i = 0; i < ROIs.size(); i++)
	{
		algorithms.push_back(createTrackerByName(trackertype));
		objects.push_back(ROIs[i]);
	}
	multiTracker.add(algorithms, AOI_show, objects);
}

int AOI_Tool::Program::LoadFixation(System::String^ fname) {
	//data part
	if (fname == "") return 0;
	std::string s = StringConverter(fname);
	fstream in;
	in.open(s);
	string line;
	int c = 0;
	while (getline(in, line, '\n'))
	{
		//Debug::WriteLine("load fixation{}", fname);
		istringstream templine(line);
		string data;
		while (getline(templine, data, ','))
		{
			Fixation_matrix.push_back(atof(data.c_str()));
		}
		c++;
	}
	in.close();
	return 1;
}

void WriteAOIData(std::string filename) {
	//************************************************************************
	ofstream out;
	out.open(filename);
	for (int i = 1; i < AOI_count; i++) {
		if (AOIarray[i].confirm == true) {
			out << "$" << "AOI_" << i << "$" << AOIarray[i].frame;
		}

	}
}

int AOI_Tool::Program::MainFunction(System::String^ videoname, int M, int A, int C, int B, int T, int Print_FLG)
{
	//parameters
	MOSAIC_SIZE = M;
	AREA_MIN = A;
	COLOR_DIFF = C;
	BANNER_HEIGHT = B;
	TOOLBAR_HEIGHT = T;
	SCENECHANGE_THRESHOLD = 30;
	DIST = 5;
	AOI_MAXNUM = 1000;

	//video version
	std::string s = StringConverter(videoname);
	//videoname = "Ch. 2.2 Introduction to Cloud Computing - Three Levels of Cloud Computing.mp4";
	VideoCapture video(s);
	if (!video.isOpened()) {
		Debug::WriteLine("video null");
		return -1;
	}
	int FPS = video.get(CAP_PROP_FPS);
	//Debug::WriteLine("FPS {0}", FPS);
	while (true) {

		video >> src;
		if (src.empty()) {
			break;
		}
		frame_count++;
		FLG_changescene = (frame_count == 1) ? 1 : 0;
		if (frame_count % 10 == 1) {

			/// Create Window
			//imshow("Source", src);
			//initialize
			mosaic = src.clone();
			scan = src.clone();
			AOI_color = src.clone();
			AOI_show = src.clone();
			tmp1 = src.clone();
			tmp2 = src.clone();
			tmp3 = src.clone();
			tmp1 = Scalar(0, 0, 0);
			tmp2 = Scalar(0, 0, 0);
			tmp3 = Scalar(0, 0, 0);

			if (SceneChange(previous, src, SCENECHANGE_THRESHOLD))  FLG_changescene = 1;

			//Mosaic(src, mosaic);
			//Scan(mosaic, scan);
			//FindAOI(scan, AOI_color, AOI_show);
			//SortAOI(AOI_show, 3);
			//ReDraw(AOI_show, Print_FLG);
			//if (Print_FLG == 1) PrintFixation(AOI_show, FPS);
			//imshow("AOI_show", AOI_show);

			//imshow("video demo", src);
			
			if (cv::waitKey(10000 / FPS) == 112 || FLG_changescene) {	// 'p' = 112 'P' = 80

				int H = src.rows / MOSAIC_SIZE;
				int W = src.cols / MOSAIC_SIZE;
				//cout << "W " << W << " H " << H << endl;
				AOImap = new int*[H + 1];
				FLGmap = new int*[H + 1];
				for (int i = 0; i < H + 1; i++) {
					AOImap[i] = new int[W + 1];
					FLGmap[i] = new int[W + 1];
				}

				Mosaic(src, mosaic);
				Scan(mosaic, scan);
				FindAOI(scan, AOI_color, AOI_show);
				SortAOI(AOI_show, 3);
				ReDraw(AOI_show, Print_FLG);
				if (Print_FLG == 1) PrintFixation(AOI_show, FPS);
				imshow("AOI_show", AOI_show);
				setMouseCallback("AOI_show", onMouse, NULL);

				for (int i = 0; i < H + 1; i++) {
					delete[] AOImap[i];
					delete[] FLGmap[i];
				}
				delete[] AOImap;
				delete[] FLGmap;

				FLG_pause = 1;
				AOI_correct = 0;
				AOI_total = 0;
				//if (FLG_changescene) cout << "scene changed, pause" << endl;
				//else cout << "pause" << endl;
				while (cv::waitKey(10000 / FPS) != 112) {}
				CalculateAcurrancy();
				TrackingSet("KCF");
				FLG_pause = 0;

				Debug::WriteLine("{0} / {1}", AOI_correct, AOI_total);
			}
			else {
				multiTracker.update(AOI_show);
				for (unsigned i = 0; i<multiTracker.getObjects().size(); i++)
					rectangle(AOI_show, multiTracker.getObjects()[i], Scalar(255, 0, 0), 2, 1);
				imshow("AOI_show", AOI_show);
			}
		}
	}

	cv::waitKey(0);
	return(0);
}