// AOI_Tool.cpp: 主要專案檔。

#include "stdafx.h"

using namespace System;
using namespace System::Windows::Forms;

[STAThreadAttribute]
int main(array<System::String ^> ^args)
{
	Application::EnableVisualStyles();
	Application::SetCompatibleTextRenderingDefault(false);
	AOI_Tool::Main_form nameObject;
	Application::Run(%nameObject);

    return 0;
}
