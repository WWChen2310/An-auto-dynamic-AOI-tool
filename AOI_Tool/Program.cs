using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace AOI_Tool
{
    public struct AOI
    {
        public int L, R, T, D;
        public int Area;
    }
    public class Global
    {
        public static Image<Bgr, byte> src;
        public static Image<Bgr, byte> src_gray;
        public static Image<Bgr, byte> previous;
        public static Image<Bgr, byte> mosaic;
        public static Image<Bgr, byte> scan; //Mat scan2;
        public static Image<Bgr, byte> AOI_color;
        public static Image<Bgr, byte> AOI_show;

        //public static vector<double> Fixation_matrix;
        //public static ArrayList Fixation_matrix = new ArrayList();
        public static double[,] Fixation_matrix = new double[2000, 3];
        public static int Fixation_count = 0;
        public static int Frame_count = 0;
        public static int AOI_count = 0;

        public static bool FLG_pause = false;
        public static bool FLG_changescene = false;
        //public static vector<double> area_c;
        //public static vector<Point2f> center_c;
        //public static int** AOImap;
        //public static int** FLGmap;
        public static int[,] AOImap = new int[200, 200];
        public static int[,] FLGmap = new int[200, 200];
        public static AOI[] AOIarray = new AOI[1000];

        public static Point eyefixation;
        public static Point vertexLU;
        public static Point vertexRD;

        public static Capture video;
        public static double FPS;

        public static int MOSAIC_SIZE = 10;
        public static int AREA_MIN = 10;
        public static int COLOR_DIFF = 10;
        public static int DIST = 5;
        public static int BANNER_HEIGHT = 0; //65
        public static int TOOLBAR_HEIGHT = 0;//55
        public static int AOI_MAXNUM = 1000;
        public static int TRACKER_TYPE = 2;
        public static int SCENECHANGE_THRESHOLD = 30;

    }

    static class Program
    {
        static bool Overlapping(AOI A1, AOI A2)
        {
            if (((A1.L <= A2.R && A1.L >= A2.L) ||
                (A1.R <= A2.R && A1.R >= A2.L)) &&
                ((A1.T <= A2.D && A1.T >= A2.T) ||
                (A1.D <= A2.D && A1.D >= A2.T)))
                return true;
            else return false;
        }
        static bool Adjent(AOI A1, AOI A2)
        {
            if (((A1.R + 1 == A2.L) && ((A2.D >= A1.T && A2.D <= A1.D) || (A2.T >= A1.T && A2.T <= A1.D))) ||
                ((A1.D + 1 == A2.T) && ((A2.L >= A1.L && A2.L <= A1.R) || (A2.R >= A1.L && A2.R <= A1.R))) ||
                ((A2.R + 1 == A1.L) && ((A1.D >= A2.T && A1.D <= A2.D) || (A1.T >= A2.T && A1.T <= A2.D))) ||
                ((A2.D + 1 == A1.T) && ((A1.L >= A2.L && A1.L <= A2.R) || (A1.R >= A2.L && A1.R <= A2.R))))
                return true;
            else return false;
        }
        static bool SceneChange(Image<Bgr, byte> pre, Image<Bgr, byte> now, int SCENECHANGE_THRESHOLD)
        {
            Bgr c1, c2;
            double s = 0, c = 0;

            for (int i = 0; i < pre.Rows; i++)
            {
                for (int j = 0; j < pre.Cols; j++)
                {
                    c1 = pre[i, j];
                    c2 = now[i, j];
                    s += Math.Pow(Math.Pow(c1.Blue - c2.Blue, 2) + Math.Pow(c1.Green - c2.Green, 2) + Math.Pow(c1.Red - c2.Red, 2), 0.5);
                    c++;
                }
            }
            double r = ((double)s) / c;
            //cout << "scenechange value: " << r << endl;
            //Console.WriteLine("scenechange value: {0}", r);
            if (r > SCENECHANGE_THRESHOLD) return true;
            else return false;
        }
        static bool IsRed(Bgr c)
        {
            if (c.Blue == 0 && c.Green == 0 && c.Red == 255)
                return true;
            else return false;
        }
        static void Reset_AOI(AOI clear)
        {
            clear.L = 0; clear.R = 0; clear.T = 0; clear.D = 0;
            clear.Area = 0;
        }
        static void Print_AOIinfo(AOI print)
        {
            //cout << "AOI L" << print.L << " R" << print.R << " T" << print.T << " D" << print.D << endl;
            //cout << "Area " << print.Area << endl;
            Console.WriteLine("AOI L {0} R {1} T {2} D {3}", print.L, print.R, print.T, print.D);
            Console.WriteLine("Area {0}", print.Area);
        }
        static void Show_Fixation(Image<Bgr, byte> img)
        {
            int interval = 0;
            //cout << "frame count : " << frame_count << endl;
            while (Global.Fixation_matrix[interval, 0] * Global.FPS / 100 < Global.Frame_count) interval++; //find time interval number
                                                                                        //cout << "time interval number : " << interval << endl;
            Global.eyefixation.X = (int)Global.Fixation_matrix[interval, 1];
            Global.eyefixation.Y = (int)Global.Fixation_matrix[interval, 2];
            //cout << "point position " << eyefixation.x << " " << eyefixation.y << endl;
            MCvScalar c = new MCvScalar(0, 0, 255);
            CvInvoke.Circle(img, Global.eyefixation, 20, c, 1);
        }
        static int dfs_visit(int m, int n, int AOI_ID)
        {
            if (Global.FLGmap[m, n] == 1) return 0;
            else if (Global.FLGmap[m, n] == 0)
            {
                Global.FLGmap[m, n] = 1;
                Global.AOImap[m, n] = AOI_ID;
                Global.AOIarray[AOI_ID].Area++;
                if (Global.FLGmap[m, n - 1] == 0)  //L
                    dfs_visit(m, n - 1, AOI_ID);
                if (Global.FLGmap[m, n + 1] == 0)  //R
                    dfs_visit(m, n + 1, AOI_ID);
                if (Global.FLGmap[m - 1, n] == 0)  //T
                    dfs_visit(m - 1, n, AOI_ID);
                if (Global.FLGmap[m + 1, n] == 0)  //D
                    dfs_visit(m + 1, n, AOI_ID);
                //8 directions
                if (Global.FLGmap[m - 1, n - 1] == 0)  //LT
                    dfs_visit(m - 1, n - 1, AOI_ID);
                if (Global.FLGmap[m + 1, n - 1] == 0)  //LD
                    dfs_visit(m + 1, n - 1, AOI_ID);
                if (Global.FLGmap[m - 1, n + 1] == 0)  //RT
                    dfs_visit(m - 1, n + 1, AOI_ID);
                if (Global.FLGmap[m + 1, n + 1] == 0)  //RD
                    dfs_visit(m + 1, n + 1, AOI_ID);
            }
            return 0;
        }
        //-------------------------------------Processes-------------------------------------------
        static void Mosaic(Image<Bgr, byte> img, Image<Bgr, byte> result, int MOSAIC_SIZE)
        {   //mosaic
            Global.previous = Global.src.Clone();
            //cout << src.cols << " " << src.cols / MOSAIC_SIZE << endl;	//1024
            //cout << src.rows << " " << src.rows / MOSAIC_SIZE << endl;	//768
            for (int m = 0; m * MOSAIC_SIZE < img.Rows; m++)
            {
                for (int n = 0; n * MOSAIC_SIZE < img.Cols; n++)
                {
                    //for each block
                    int B = 0; int G = 0; int R = 0; int pn = 0; Bgr color;
                    for (int i = 0; i < MOSAIC_SIZE && i + m * MOSAIC_SIZE < img.Rows; i++)
                    {
                        for (int j = 0; j < MOSAIC_SIZE && j + n * MOSAIC_SIZE < img.Cols; j++)
                        {
                            color = img[i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE];
                            B += (int)color.Blue;
                            G += (int)color.Green;
                            R += (int)color.Red;
                            pn++;
                            //cout <<"at " << i + m * MOSAIC_SIZE <<","<< j + n * MOSAIC_SIZE << "B " << blue << " G " << green << " R " << red << endl;
                        }
                    }
                    //cout << "block " << m << "," << n << "B " << B << " G " << G << " R " << R  << " pn" << pn  << endl;
                    B = B / pn;
                    G = G / pn;
                    R = R / pn;
                    //cout << "block " << m << "," << n << "B " << B << " G " << G << " R " << R << " pn" << pn << endl;
                    for (int i = 0; i < MOSAIC_SIZE && i + m * MOSAIC_SIZE < img.Rows; i++)
                    {
                        for (int j = 0; j < MOSAIC_SIZE && j + n * MOSAIC_SIZE < img.Cols; j++)
                        {
                            //cout << "point " << i + m * MOSAIC_SIZE << "," << j + n * MOSAIC_SIZE << "B " << B << " G " << G << " R " << R << endl;
                            //result[i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE] = B;
                            //result[i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE] = G;
                            //result[i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE] = R;
                            result[i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE] = new Bgr(B, G, R);
                            //cout <<"at " << i + m * MOSAIC_SIZE <<","<< j + n * MOSAIC_SIZE << "B " << blue << " G " << green << " R " << red << endl;
                        }
                    }
                }
            }
            //CvInvoke.Imshow("mosaic", result);
        }
        static void Scan(Image<Bgr, byte> img, Image<Bgr, byte> result, int COLOR_DIFF, int DIST)
        {
            Bgr c1, c2;

            for (int i = 0; i < img.Rows; i++)
            {
                int flagj = 0;
                for (int j = 0; j < img.Cols && j < img.Cols - 1; j++)
                {
                    c1 = img[i, j];
                    c2 = img[i, j + 1];
                    if (Math.Pow(Math.Pow(c1.Blue - c2.Blue, 2) + Math.Pow(c1.Green - c2.Green, 2) + Math.Pow(c1.Red - c2.Red, 2), 0.5) <= COLOR_DIFF)
                    {
                        flagj++;
                    }
                    else if (flagj >= DIST)
                    {
                        //cout << "find edge " << j << "," << i << endl;
                        //result.at<Vec3b>(i, j).val[0] = 0;
                        //result.at<Vec3b>(i, j).val[1] = 0;
                        //result.at<Vec3b>(i, j).val[2] = 255;
                        result[i, j] = new Bgr(0, 0, 255);
                        flagj = 0;
                    }
                }
            }
            for (int j = 0; j < img.Cols; j++)
            {
                int flagi = 0;
                for (int i = 0; i < img.Rows && i < img.Rows - 1; i++)
                {
                    c1 = img[i, j];
                    c2 = img[i + 1, j];
                    if (Math.Pow(3 * Math.Pow(c1.Blue - c2.Blue, 2) + 4 * Math.Pow(c1.Green - c2.Green, 2) + 2 * Math.Pow(c1.Red - c2.Red, 2), 0.5) <= COLOR_DIFF)
                    {
                        flagi++;
                    }
                    else if (flagi >= DIST)
                    {
                        //result.at<Vec3b>(i, j).val[0] = 0;
                        //result.at<Vec3b>(i, j).val[1] = 0;
                        //result.at<Vec3b>(i, j).val[2] = 255;
                        result[i, j] = new Bgr(0, 0, 255);
                        flagi = 0;
                    }
                }
            }
            //CvInvoke.Imshow("scan", result);
        }
        static void FindAOI(Image<Bgr, byte> img, Image<Bgr, byte> AOI_color, Image<Bgr, byte> result, int MOSAIC_SIZE, int AOI_MAXNUM, int BANNER_HEIGHT, int TOOLBAR_HEIGHT)
        {
            //initialize
            for (int m = 0; m * MOSAIC_SIZE < img.Rows; m++)
            {
                for (int n = 0; n * MOSAIC_SIZE < img.Cols; n++)
                {
                    Global.AOImap[m, n] = -1;
                    Global.FLGmap[m, n] = -1;
                }
            }
            for (int i = 0; i < AOI_MAXNUM; i++) Reset_AOI(Global.AOIarray[i]);

            //part1 在AOI_color上劃出區域
            for (int m = (BANNER_HEIGHT % MOSAIC_SIZE == 0) ? BANNER_HEIGHT / MOSAIC_SIZE + 1 : BANNER_HEIGHT / MOSAIC_SIZE; (m + 1) * MOSAIC_SIZE < img.Rows - TOOLBAR_HEIGHT; m++)
            {   //去掉上下
                for (int n = 1; (n + 1) * MOSAIC_SIZE < img.Cols; n++)
                {   //第一個&最後一個不算 從第二個開始
                    //for each block
                    Bgr c1 = img[5 + m * MOSAIC_SIZE, 9 + (n - 1) * MOSAIC_SIZE];//L
                    Bgr c2 = img[5 + m * MOSAIC_SIZE, 9 + n * MOSAIC_SIZE];//R
                    Bgr c3 = img[9 + (m - 1) * MOSAIC_SIZE, 5 + n * MOSAIC_SIZE];//U
                    Bgr c4 = img[9 + m * MOSAIC_SIZE, 5 + n * MOSAIC_SIZE];//D
                    if ((IsRed(c1) && IsRed(c2) && IsRed(c3)) || (IsRed(c1) && IsRed(c2) && IsRed(c4)) || (IsRed(c1) && IsRed(c3) && IsRed(c4)) || (IsRed(c2) && IsRed(c3) && IsRed(c4)))
                    {
                        //整個塗紅
                        //AOImap[m][n] = AOI_count;
                        Global.FLGmap[m, n] = 0;
                        for (int i = 0; i < MOSAIC_SIZE && i + (m + 1) * MOSAIC_SIZE < img.Rows; i++)
                        {
                            for (int j = 0; j < MOSAIC_SIZE && j + (n + 1) * MOSAIC_SIZE < img.Cols; j++)
                            {
                                //cout << "point " << i + m * MOSAIC_SIZE << "," << j + n * MOSAIC_SIZE << "B " << B << " G " << G << " R " << R << endl;
                                //AOI_color.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[0] = 0;
                                //AOI_color.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[1] = 0;
                                //AOI_color.at<Vec3b>(i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE)[2] = 255;
                                AOI_color[i + m * MOSAIC_SIZE, j + n * MOSAIC_SIZE] = new Bgr(0, 0, 255);
                                //cout <<"at " << i + m * MOSAIC_SIZE <<","<< j + n * MOSAIC_SIZE << "B " << blue << " G " << green << " R " << red << endl;
                            }
                        }
                    }

                }
            }

            //CvInvoke.Imshow("AOI_colored", AOI_color);
            //part2
            Global.AOI_count = 0;

            for (int m = (BANNER_HEIGHT % MOSAIC_SIZE == 0) ? BANNER_HEIGHT / MOSAIC_SIZE + 1 : BANNER_HEIGHT / MOSAIC_SIZE; (m + 1) * MOSAIC_SIZE < img.Rows - TOOLBAR_HEIGHT; m++)
            {   //第一個&最後一個不算 從第二個開始
                for (int n = 1; (n + 1) * MOSAIC_SIZE < img.Cols; n++)
                {
                    //flag_start = (FLGmap[m][n] == 0) ? 1 : 0;
                    if (Global.FLGmap[m, n] == 0)
                    {
                        Global.AOI_count++;
                        dfs_visit(m, n, Global.AOI_count);
                    }
                }
            }
            //cout << "AOI total" << AOI_count << endl;

            /*for (int m = 1; (m + 1) * MOSAIC_SIZE < img.rows; m++) {
            for (int n = 1; (n + 1) * MOSAIC_SIZE < img.cols; n++) {
            if (FLGmap[m][n] == 1 && AOImap[m][n] == 2)
            cout << "m" << m << " n" << n << endl;
            }
            }*/

            //part3 show the result
            //Print_fixation(AOI_show);
            for (int i = 1; i <= Global.AOI_count; i++)
            {   //find L,R,T,D
                //int L = 0; int R = 0; int T = 0; int D = 0;
                for (int m = (BANNER_HEIGHT % MOSAIC_SIZE == 0) ? BANNER_HEIGHT / MOSAIC_SIZE + 1 : BANNER_HEIGHT / MOSAIC_SIZE; (m + 1) * MOSAIC_SIZE < img.Rows - TOOLBAR_HEIGHT; m++)
                {   //第一個&最後一個不算
                    for (int n = 1; (n + 1) * MOSAIC_SIZE < img.Cols; n++)
                    {
                        if (Global.FLGmap[m, n] == 1 && Global.AOImap[m, n] == i)
                        {
                            Global.AOIarray[i].L = (Global.AOIarray[i].L == 0) ? n : (n < Global.AOIarray[i].L) ? n : Global.AOIarray[i].L;
                            Global.AOIarray[i].R = (Global.AOIarray[i].R == 0) ? n : (n > Global.AOIarray[i].R) ? n : Global.AOIarray[i].R;
                            Global.AOIarray[i].T = (Global.AOIarray[i].T == 0) ? m : (m < Global.AOIarray[i].T) ? m : Global.AOIarray[i].T;
                            Global.AOIarray[i].D = (Global.AOIarray[i].D == 0) ? m : (m > Global.AOIarray[i].D) ? m : Global.AOIarray[i].D;
                        }
                    }
                }
                /*for (int m = U; m <= D; m++)
                for (int n = L; n <= R; n++)
                AOImap[m][n] = i;*/
                /*int flag_in = 0;
                if (AOIarray[i].L * MOSAIC_SIZE < eyefixation.x && (AOIarray[i].R + 1) * MOSAIC_SIZE > eyefixation.x && AOIarray[i].T * MOSAIC_SIZE < eyefixation.y && (AOIarray[i].D + 1) * MOSAIC_SIZE > eyefixation.y) flag_in = 1;
                if (AOIarray[i].Area >= AREA_MIN) {
                if (flag_in) {
                rectangle(AOI_show, Point(AOIarray[i].L * MOSAIC_SIZE, AOIarray[i].T * MOSAIC_SIZE), Point((AOIarray[i].R + 1) * MOSAIC_SIZE, (AOIarray[i].D + 1) * MOSAIC_SIZE), Scalar(0, 0, 255), 3);
                }
                else rectangle(AOI_show, Point(AOIarray[i].L * MOSAIC_SIZE, AOIarray[i].T * MOSAIC_SIZE), Point((AOIarray[i].R + 1) * MOSAIC_SIZE, (AOIarray[i].D + 1) * MOSAIC_SIZE), Scalar(0, 255, 255), 3);

                }*/
            }

            //CvInvoke.Imshow("AOI_show", result);
        }
        static void SortAOI(Image<Bgr, byte> AOI_show, int MOSAIC_SIZE, int AREA_MIN)
        {
            for (int n = 0; n < 3; n++)
            {
                for (int i = 0; i < Global.AOI_count; i++)
                {
                    for (int j = 0; j < Global.AOI_count; j++)
                    {
                        if (Overlapping(Global.AOIarray[i], Global.AOIarray[j]) || Adjent(Global.AOIarray[i], Global.AOIarray[j]))
                        {
                            Global.AOIarray[i].R = Global.AOIarray[i].R > Global.AOIarray[j].R ? Global.AOIarray[i].R : Global.AOIarray[j].R;
                            Global.AOIarray[i].L = Global.AOIarray[i].L < Global.AOIarray[j].L ? Global.AOIarray[i].L : Global.AOIarray[j].L;
                            Global.AOIarray[i].T = Global.AOIarray[i].T < Global.AOIarray[j].T ? Global.AOIarray[i].T : Global.AOIarray[j].T;
                            Global.AOIarray[i].D = Global.AOIarray[i].D > Global.AOIarray[j].D ? Global.AOIarray[i].D : Global.AOIarray[j].D;
                            //AOIarray[i].Area = AOIarray[i].Area + AOIarray[j].Area;
                            //AOIarray[j].Area = AOIarray[i].Area + AOIarray[j].Area;
                        }
                    }
                }
            }

            for (int i = 0; i < Global.AOI_count; i++)
            {
                int flag_in = 0;
                if (Global.AOIarray[i].L * MOSAIC_SIZE < Global.eyefixation.X && (Global.AOIarray[i].R + 1) * MOSAIC_SIZE > Global.eyefixation.X && Global.AOIarray[i].T * MOSAIC_SIZE < Global.eyefixation.Y && (Global.AOIarray[i].D + 1) * MOSAIC_SIZE > Global.eyefixation.Y) flag_in = 1;
                if (Global.AOIarray[i].Area >= AREA_MIN)
                {
                    if (flag_in != 0)
                    {
                        //Rectangle rct = new Rectangle(new Point(Global.AOIarray[i].L * MOSAIC_SIZE, Global.AOIarray[i].T * MOSAIC_SIZE), new Point((Global.AOIarray[i].R + 1) * MOSAIC_SIZE, (Global.AOIarray[i].D + 1) * MOSAIC_SIZE));
                        //CvInvoke.Rectangle(AOI_show, rct, new MCvScalar(0, 0, 255), 3);
                        AOI_show.Draw(new Rectangle(new Point(Global.AOIarray[i].L * MOSAIC_SIZE, Global.AOIarray[i].T * MOSAIC_SIZE), new Size((Global.AOIarray[i].R - Global.AOIarray[i].L + 1) * MOSAIC_SIZE, (Global.AOIarray[i].D - Global.AOIarray[i].T + 1) * MOSAIC_SIZE)), new Bgr(0, 0, 255), 3);
                    }
                    else
                    {
                        //Rectangle rct = new Rectangle(new Point(Global.AOIarray[i].L * MOSAIC_SIZE, Global.AOIarray[i].T * MOSAIC_SIZE), Point((Global.AOIarray[i].R + 1) * MOSAIC_SIZE, (Global.AOIarray[i].D + 1) * MOSAIC_SIZE));
                        //CvInvoke.Rectangle(AOI_show, Point(AOIarray[i].L * MOSAIC_SIZE, AOIarray[i].T * MOSAIC_SIZE), Point((AOIarray[i].R + 1) * MOSAIC_SIZE, (AOIarray[i].D + 1) * MOSAIC_SIZE), Scalar(0, 255, 0), 3);
                        AOI_show.Draw(new Rectangle(new Point(Global.AOIarray[i].L * MOSAIC_SIZE, Global.AOIarray[i].T * MOSAIC_SIZE), new Size((Global.AOIarray[i].R - Global.AOIarray[i].L + 1) * MOSAIC_SIZE, (Global.AOIarray[i].D - Global.AOIarray[i].T + 1) * MOSAIC_SIZE)), new Bgr(0, 255, 0), 3);
                    }
                }
            }
            CvInvoke.Imshow("AOI_show", AOI_show);
        }
        static void Tester()
        {
            /*Image<Bgr, byte> image;
            image = new Image<Bgr, byte>("C:\\Users\\wei-wen chen\\Documents\\Visual Studio 2015\\Projects\\WindowsFormsApplication1\\WindowsFormsApplication1\\123.jpg");
            CvInvoke.Imshow("Test", image); //显示图片
            CvInvoke.WaitKey(0); //等待按键输入
            CvInvoke.DestroyWindow("Test");
            image.Dispose();*/

            //Mat m = new Mat(520, 200, DepthType.Cv8U, 3);
            //Image<Bgr, byte> im = m.ToImage<Bgr, byte>();

            Image<Bgr, byte> image = new Image<Bgr, byte>(520, 200);
            Console.WriteLine("Rows {0}", image.Rows);//200
            Console.WriteLine("Cols {0}", image.Cols);//520
            for (int i = 0; i < image.Rows; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    image[i, j] = new Bgr(0, 255, 0);
                }
            }
            Console.WriteLine("Color {0}", image[10, 10]);//520
            CvInvoke.Imshow("Test", image); //显示图片
            CvInvoke.WaitKey(0); //等待按键输入
            CvInvoke.DestroyWindow("Test");
            image.Dispose();
        }
        public static void LoadData(string filePath)
        {
            if(filePath == "") 
                filePath = "C:\\Users\\wei-wen chen\\Documents\\Visual Studio 2015\\Projects\\WindowsFormsApplication1\\WindowsFormsApplication1\\Fixationdata.csv";
            //string filePath = "C:\\Users\\wei-wen chen\\Documents\\Visual Studio 2015\\Projects\\WindowsFormsApplication1\\WindowsFormsApplication1\\Fixationdata.csv";
            DataTable dt = new DataTable();
            FileStream fs = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] words = line.Split(',');
                    //Console.WriteLine("{0} {1} {2}",words[0], words[1], words[2]);
                    Global.Fixation_matrix[Global.Fixation_count, 0] = Convert.ToDouble(words[0]);
                    Global.Fixation_matrix[Global.Fixation_count, 1] = Convert.ToDouble(words[1]);
                    Global.Fixation_matrix[Global.Fixation_count, 2] = Convert.ToDouble(words[2]);
                    //Console.WriteLine("{0} {1} {2}", Global.Fixation_matrix[i, 0], Global.Fixation_matrix[i, 1], Global.Fixation_matrix[i, 2]);
                    Global.Fixation_count++;
                }
                
            }
            //Console.WriteLine("fixation count {0}", Global.Fixation_count);

        }
        public static void LoadVideo(string videoname)
        {
            if(videoname == "")
                videoname = "C:\\Users\\wei-wen chen\\Documents\\Visual Studio 2015\\Projects\\WindowsFormsApplication1\\WindowsFormsApplication1\\Ch. 2.2 Introduction to Cloud Computing - Three Levels of Cloud Computing.mp4";
            Global.video = new Capture(videoname);
            Global.FPS = Global.FPS = Global.video.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
        }
        public static void SetParameters(int MOSAIC_SIZE, int AREA_MIN, int COLOR_DIFF, int BANNER_HEIGHT, int TOOLBAR_HEIGHT)
        {
            Global.MOSAIC_SIZE = MOSAIC_SIZE;
            Global.AREA_MIN = AREA_MIN;
            Global.COLOR_DIFF = COLOR_DIFF;
            Global.DIST = MOSAIC_SIZE / 2;
            Global.BANNER_HEIGHT = BANNER_HEIGHT;
            Global.TOOLBAR_HEIGHT = TOOLBAR_HEIGHT;
            //Global.AOI_MAXNUM = AOI_MAXNUM;
            //Global.TRACKER_TYPE = TRACKER_TYPE;
            //Global.SCENECHANGE_THRESHOLD = SCENECHANGE_THRESHOLD;

        }
        public static void MainProcess()
        {
            //LoadData("");
            //LoadVideo("");
            //SetParameters();
            //Size videoSize = new Size((int)video.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth),(int)video.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight));
            int delay = (int)(1000 / Global.FPS);
            while (true)
            {
                Mat frame = Global.video.QueryFrame();
                if (frame == null)
                {
                    Console.WriteLine("frame null");
                    break;
                }

                
                Global.Frame_count++;
                Global.FLG_changescene = (Global.Frame_count == 1) ? true : false;
                if(Global.Frame_count % 10 == 1)
                {
                    string windowTitle = "Source Video";
                    CvInvoke.NamedWindow(windowTitle);
                    CvInvoke.Imshow(windowTitle, frame);

                    //initialize
                    int H = frame.Rows / Global.MOSAIC_SIZE;
                    int W = frame.Cols / Global.MOSAIC_SIZE;
                    //dynamic


                    Global.src = frame.ToImage<Bgr, byte>();
                    Global.mosaic = Global.src.Clone();
                    Global.scan = Global.src.Clone();
                    Global.AOI_color = Global.src.Clone();
                    Global.AOI_show = Global.src.Clone();

                    if(!Global.FLG_changescene) Global.FLG_changescene = SceneChange(Global.previous, Global.src, Global.SCENECHANGE_THRESHOLD);

                    Mosaic(Global.src, Global.mosaic, Global.MOSAIC_SIZE);
                    Scan(Global.mosaic, Global.scan, Global.COLOR_DIFF, Global.DIST);
                    FindAOI(Global.scan, Global.AOI_color, Global.AOI_show, Global.MOSAIC_SIZE, Global.AOI_MAXNUM, Global.BANNER_HEIGHT, Global.TOOLBAR_HEIGHT);
                    SortAOI(Global.AOI_show, Global.MOSAIC_SIZE, Global.AREA_MIN);

                    //reset AOImap and FLGmap


                    if(CvInvoke.WaitKey(delay) == 112 || Global.FLG_changescene)
                    {
                        Global.FLG_pause = true;
                        if (Global.FLG_changescene) Console.WriteLine("Scene Change Pause");
                        else Console.WriteLine("User Pause");
                        while(CvInvoke.WaitKey(delay) != 112) { }
                        Global.FLG_pause = false;
                        Console.WriteLine("User Restart");
                    }
                }

            }
        }
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        /*static void Main()
        {
            MainProcess();
            
        }*/
    }
}
