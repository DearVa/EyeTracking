#pragma once
#include <libusbk.h>

class UsbKCapture
{
public:
    ~UsbKCapture();
    static int EnumUsbDevices(char*** names);
    bool OpenDevice(int dev_id);
    int width();
    int height();
    int device_id;
    void Start(void (*handler)(PUCHAR, UINT));
    void Stop();
    bool CloseDevice() const;

private:
    bool ResetFPGA();
    void Loop() const;
    void (*handler)(PUCHAR, UINT) = nullptr;
    static KLST_HANDLE device_list;
    static KLST_DEVINFO_HANDLE device_info;
    KUSB_DRIVER_API usb;
    KUSB_HANDLE handle;
};
