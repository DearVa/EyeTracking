#include "pch.h"
#include "UsbKCapture.h"

#include <thread>

UsbKCapture::~UsbKCapture()
{
    auto _ = CloseDevice();
}

int UsbKCapture::EnumUsbDevices(char *** names)
{
    constexpr ULONG vid_arg = 0x04B4;
    constexpr ULONG pid_arg = 0x1004;
    UINT device_count = 0;

    //if (handle) usb->Free(handle);

    if (device_list)
    {
        LstK_Free(device_list);
    }

    // Get the device list
    if (!LstK_Init(&device_list, KLST_FLAG_NONE)) return false;

    LstK_Count(device_list, &device_count);

    if (!device_count)
    {
        LstK_Free(device_list);
        device_list = nullptr;
        device_info = nullptr;
        return 0;
    }

    LstK_FindByVidPid(device_list, vid_arg, pid_arg, &device_info);

    if (!device_info)
    {
        LstK_Free(device_list);
        return 0;
    }
    *names = new char*[device_count];
    for (UINT i = 0; i < device_count; i++)
    {
        //names[i] = (device_info->DeviceDesc);
    }

    return device_count;
}

bool UsbKCapture::OpenDevice(const int dev_id)
{
    this->device_id = dev_id;

    LibK_LoadDriverAPI(&usb, dev_id);

    if (device_info == nullptr) return false;

    if (!usb.Init(&handle, device_info)) return false;

    ResetFPGA();

    Sleep(50);
    usb.ResetPipe(handle, 0x82);
    usb.ResetPipe(handle, 0x88);
    usb.ResetPipe(handle, 0x06);
    Sleep(50);

    ULONG timeout = 100;
    usb.SetPipePolicy(handle, 0x82, PIPE_TRANSFER_TIMEOUT, sizeof(ULONG), &timeout);
    usb.SetPipePolicy(handle, 0x88, PIPE_TRANSFER_TIMEOUT, sizeof(ULONG), &timeout);
    usb.SetPipePolicy(handle, 0x06, PIPE_TRANSFER_TIMEOUT, sizeof(ULONG), &timeout);

    return handle != nullptr;
}

#define  IMG_WIDTH  800
#define  IMG_HEIGHT 600

int UsbKCapture::width()
{
    return IMG_WIDTH;
}

int UsbKCapture::height()
{
    return IMG_HEIGHT;
}

void UsbKCapture::Start(void (*handler)(PUCHAR, UINT))
{
    if (this->handler) return;
    this->handler = handler;
    //auto thread = std::thread(&Loop,this);
    //thread.join();
}

void UsbKCapture::Stop()
{
    if (!handler) return;
    handler = nullptr;
}


bool UsbKCapture::CloseDevice() const
{
    if (handle) usb.Free(handle);
    return true;
}

typedef enum { REQ_STD, REQ_CLASS, REQ_VENDOR } CTL_XFER_REQ_TYPE;

#define	USER_VENDOR_REQUEST	0x80			//	User vendor request to switch operation modes. 
#define	OPERATOR_MODE_GPIO	0x00
#define	OPERATOR_MODE_FIFO	0x01

bool UsbKCapture::ResetFPGA()
{
    constexpr int len = 0;
    BYTE data;
    WINUSB_SETUP_PACKET setup;
    setup.RequestType = 0x0 << 7 | REQ_VENDOR << 5 | 0x0;
    setup.Request = USER_VENDOR_REQUEST; // 0xa9;
    setup.Value = OPERATOR_MODE_GPIO;
    setup.Index = 0;
    setup.Length = 0;
    usb.ControlTransfer(handle,
                         setup,
                         &data,
                         len,
                         nullptr,
                         nullptr);

    Sleep(50);
    setup.Value = OPERATOR_MODE_FIFO;
    return usb.ControlTransfer(handle,
                                setup,
                                &data,
                                len,
                                nullptr,
                                nullptr);
}


#define  CAM_NUM 1
#define  BYTES_PER_FRAME (IMG_WIDTH*IMG_HEIGHT*CAM_NUM)

void UsbKCapture::Loop() const
{
    UINT length = 0;

    const auto buffer = new BYTE[BYTES_PER_FRAME * 3 + 4096];
    while (handler)
    {
        this->usb.ReadPipe(handle,
                            0x82,
                            buffer,
                            BYTES_PER_FRAME,
                            &length,
                            nullptr);

        if (length == BYTES_PER_FRAME && handler)
        {
            handler(buffer, length);
        }
    }
    delete[] buffer;
}
