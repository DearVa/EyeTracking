#pragma once
#include "libusbk.h"

namespace EyeTracking
{
    namespace Windows
    {
        namespace Capture
        {
            using namespace System;
            public ref class UsbKCapture
            {
            public:
                delegate void DataHandler(BYTE* buffer,UINT length);
                ~UsbKCapture();
                static int EnumUsbDevices(out array<String^>^ % names);
                bool OpenDevice(int dev_id);
                property int Width
                {
                    int get();
                }
                property int Height
                {
                    int get();
                }
                property int DeviceId
                {
                    int get(); 
                };
                void Start(DataHandler^ handler);
                void Stop();
                bool CloseDevice();
            private:
                bool ResetFPGA();
                void Loop();
                DataHandler^ handler;
                Threading::Tasks::Task^ task;
                static KLST_HANDLE* device_list = new KLST_HANDLE();
                static KLST_DEVINFO_HANDLE* device_info = new KLST_DEVINFO_HANDLE();
                KUSB_DRIVER_API* usb = new KUSB_DRIVER_API();
                KUSB_HANDLE* handle = new KUSB_HANDLE();
                int dev_id;
            };
        }
    }
}
