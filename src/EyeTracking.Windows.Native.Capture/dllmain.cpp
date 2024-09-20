// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"

#include "CameraCapture.h"

BOOL APIENTRY DllMain(HMODULE hModule,
                      DWORD ul_reason_for_call,
                      LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

extern "C" __declspec(dllexport) void* Run(const HANDLE handle, void (*handler)(unsigned char*, long))
{
    const auto instance = new CameraCapture(handle, handler);
    return instance;
}

extern "C" __declspec(dllexport) void Stop(void* pointer)
{
    const auto instance = (CameraCapture*)pointer;
    instance->stop();
}
