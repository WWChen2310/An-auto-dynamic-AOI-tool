本工具使用opencv以色差法分析影片，以10個frame一次的頻率進行畫面更新。將自動抓取出影片中值得關注之重點區域(AOI)，並可以讓使用者自行定義正確的AOI，後續將追蹤AOI在畫面上的移動。

安裝：
1. 利用Cmake安裝 OpenCV以及contrib
可參考https://blog.csdn.net/liu798675179/article/details/51259505
OpenCV 3.4.3
https://opencv.org/releases.html
OpenCV contrib
https://github.com/opencv/opencv_contrib
Cmake
https://cmake.org/download/

開發使用版本：
Window 10 x64
visual studio 2015(vc14)
OpenCV 3.4.3
OpenCV contrib 4.0.0
Cmake 3.13

2.
opencv Path:C:/opencv
opencv contrib Path:D:/opencv

3.
自行調整環境設定
屬性頁->VC++ 目錄->Include目錄
調整為
”opencv path\build\include\opencv2;
opencv path\build\include;
opencv cotrib path\install\include\opencv2;
opencv cotrib path\install\include”

屬性頁->VC++ 目錄->程式庫目錄
調整為
”opencv path\build\x64\vc14\lib;
opencv contrib path\install\x64\vc14\lib;”

屬性頁->連結器->輸入->其他相依性
調整為
"opencv_world343.lib;
opencv_world343d.lib;
opencv_tracking400.lib;
opencv_tracking400d.lib;"
重新編譯

使用說明：
※Main form
-Video: 選擇要分析之影片
-Fixation: 選擇該影片之fixation data 若有選擇則會將fixation位置同步顯示在影片上
-Video Type: 對應不同類型的影片 演算法之參數會不同(未實作)

※Setting form
-Mosaic size: 分析影片時使用的方格大小 預設10 * 10 pixels
-Minimun area: 最小採用的AOI面積 單位為方格數
-Color difference: 採用AOI時的最小邊界色差
-Banner height: 遮蔽上方區域高度 在側錄螢幕類型影片時使用
-Toolbar height: 遮蔽下方區域高度 在側錄螢幕類型影片時使用

※AOI show
-P: 暫停/撥放影片 在暫停模式下可以修改AOI
-滑鼠左鍵: 在暫停模式下 可以拖曳出AOI區域
-滑鼠右鍵: 標註自動選取AOI的正確與否 標註為正確者才會進行物件追蹤
-AOI顏色: 
綠色-程式自動抓取之AOI  
紅色-使用者標註為正確之AOI 
藍色-物件追蹤中之AOI


Future work:
-測試不同影片類型找出適合不同類型影片之參數
預計測試: 瀏覽網路之螢幕側錄影片、教學影片、廣告影片
-輸出分析結果至檔案
