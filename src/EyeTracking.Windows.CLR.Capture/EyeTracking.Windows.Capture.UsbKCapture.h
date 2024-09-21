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
                static int EnumUsbDevices(array<String^>^* names);
                bool OpenDevice(int dev_id);
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
                static KLST_HANDLE* device_list;
                static KLST_DEVINFO_HANDLE* device_info;
                KUSB_DRIVER_API* usb;
                KUSB_HANDLE* handle;
                int dev_id;
            };
        }
    }
}
