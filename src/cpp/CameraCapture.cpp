#include "CameraCapture.h"
static constexpr int queue_size = 64;
static long bytes_xfer = 307200; //640*480

UINT loop(const LPVOID arg)
{
    int i;
    LONG length = 0;
    const auto ctrl_buf = new UCHAR;
    constexpr long len = 307200;

    // Allocate the arrays needed for queueing
    const auto buffers = new PUCHAR[queue_size];
    const auto contexts = new PUCHAR[queue_size];
    OVERLAPPED in_ov_lap[64];

    const auto context = (CameraCapture*)arg;
    context->in_end_point->TimeOut = 100;
    context->in_end_point->SetXferSize(len);

    // Allocate all the buffers for the queues
    for (i = 0; i < queue_size; i++)
    {
        buffers[i] = new UCHAR[len];
        in_ov_lap[i].hEvent = CreateEvent(nullptr,
                                          false,
                                          false,
                                          LPCWSTR("CYUSB_IN"));
        memset(buffers[i], 0, len);
    }
    auto ept = context->device->ControlEndPt; //USB	
    ept->Target = TGT_DEVICE;
    ept->ReqType = REQ_VENDOR;
    ept->Direction = DIR_TO_DEVICE;
    ept->ReqCode = 0xa8;
    ept->Value = 0;
    ept->Index = 0;
    ept->XferData(ctrl_buf, length);

    // Queue-up the first batch of transfer requests
    for (i = 0; i < queue_size; i++)
    {
        contexts[i] = context->in_end_point->BeginDataXfer(buffers[i], len, &in_ov_lap[i]);
        if (context->in_end_point->NtStatus || context->in_end_point->UsbdStatus) // BeginDataXfer failed
        {
            context->abort_xfer_loop(i + 1,
                                     true,
                                     buffers,
                                     contexts,
                                     in_ov_lap);
            return true;
        }
    }

    i = 0;

    while (context->looping) //循环
    {
        auto r_len = len; // Reset this each time through because
        // FinishDataXfer may modify it

        if (!context->in_end_point->WaitForXfer(&in_ov_lap[i], 2000))
        {
            context->in_end_point->Abort();
            if (context->in_end_point->LastError == ERROR_IO_PENDING)
                WaitForSingleObject(in_ov_lap[i].hEvent, 2000);
        }

        if (context->in_end_point->FinishDataXfer(buffers[i], r_len, &in_ov_lap[i], contexts[i]))
        {
            context->raw_index += r_len;
            if (context->raw_index == len)
            {
                context->handler(buffers[i], len);
                context->raw_index = 0;
            }
        }
        else
        {
            context->looping = false;
        }

        // Re-submit this queue element to keep the queue full
        contexts[i] = context->in_end_point->BeginDataXfer(buffers[i], r_len, &in_ov_lap[i]);
        if (context->in_end_point->NtStatus || context->in_end_point->UsbdStatus) // BeginDataXfer failed
        {
            context->abort_xfer_loop(queue_size, true, buffers, contexts, in_ov_lap);
            return true;
        }

        i++;

        if (i == queue_size) //Only update the display once each time through the Queue
        {
            i = 0;
        }
    }

    //此处可以不做帧同步
    context->abort_xfer_loop(queue_size, false, buffers, contexts, in_ov_lap);
    ept = context->device->ControlEndPt; //USB	
    ept->Target = TGT_DEVICE;
    ept->ReqType = REQ_VENDOR;
    ept->Direction = DIR_TO_DEVICE;
    ept->ReqCode = 0xa9;
    ept->Value = 0;
    ept->Index = 0;
    ept->XferData(ctrl_buf, length);
    return true;
}

CameraCapture::CameraCapture(const HANDLE handle, void (*handler)(PUCHAR, LONG))
{
    this->handler = handler;
    this->header = new unsigned char [40];
    create_header(this->header, 480, 640);
    device = new CCyUSBDevice(handle);
}

void CameraCapture::start()
{
    long length = 0;
    const auto ctrl_buf = new UCHAR;
    //-------------------------------------------------
    //发送帧同步命令
    const auto edp = end_point = device->ControlEndPt; //USB	
    edp->Target = TGT_DEVICE;
    edp->ReqType = REQ_VENDOR;
    edp->Direction = DIR_TO_DEVICE;
    edp->ReqCode = 0xa9;
    edp->Value = 0;
    edp->Index = 0;
    edp->XferData(ctrl_buf, length);

    //------------------------------------------------
    device->Open(0); //外链接库（lib） 打开USB端口
    const int end_point_count = device->EndPointCount();

    for (int i = 1; i < end_point_count; i++)
    {
        const auto b_in = device->EndPoints[i]->bIn;
        const auto b_bulk = device->EndPoints[i]->Attributes == 2;
        if (b_bulk && b_in) in_end_point = (CCyBulkEndPoint*)device->EndPoints[i];
    }

    if (device->IsOpen()) //判断是否打开USB
    {
        const auto t = thread = new std::thread(loop, this);
        t->detach();
    }
}

void CameraCapture::stop()
{
    if (!thread)return;
    looping = false;
    thread = nullptr;
}


void CameraCapture::abort_xfer_loop(
    const int pending,
    const bool opt,
    const PUCHAR* buffers,
    const PUCHAR* contexts,
    OVERLAPPED in_ov_lap[])
{
    in_end_point->Abort();

    if (!opt)
    {
        for (int j = 0; j < queue_size; j++)
        {
            CloseHandle(in_ov_lap[j].hEvent);
            delete [] buffers[j];
        }
    }
    else
    {
        for (int j = 0; j < queue_size; j++)
        {
            if (j < pending)
            {
                if (!in_end_point->WaitForXfer(&in_ov_lap[j], 2000))
                {
                    in_end_point->Abort();
                    if (in_end_point->LastError == ERROR_IO_PENDING)
                        WaitForSingleObject(in_ov_lap[j].hEvent, 2000);
                }

                in_end_point->FinishDataXfer(buffers[j], bytes_xfer, &in_ov_lap[j], contexts[j]);
            }

            CloseHandle(in_ov_lap[j].hEvent);
            delete [] buffers[j];
        }
    }

    delete [] buffers;
    delete [] contexts;

    //-----------------------------------------------------
    looping = false;
}

void CameraCapture::create_header(unsigned char* header, const int height, const int width)
{
    const unsigned char height_low = height & 0x00FF;
    const unsigned char height_high = (height & 0xFF00) >> 8;
    const unsigned char width_low = width & 0x00FF;
    const unsigned char width_high = (width & 0xFF00) >> 8;

    unsigned char* p = header;

    //4 Bytes -- Size of InfoHeader =40 
    *p = 40;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    //4 Bytes -- specifies the width of the image, in pixels.
    *p = width_low;
    p++;
    *p = width_high;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    //4 Bytes -- specifies the heigth of the image, in pixels.
    *p = height_low;
    p++;
    *p = height_high;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    //2 Bytes -- Number of planes of the image
    *p = 1;
    p++;
    *p = 0;
    p++;
    //2 Bytes Bits per Pixel  -- In our case 8.
    *p = 24;
    p++;
    *p = 0;
    p++;
    //4 bytes -- Type of Compression 0 = BI_RGB   no compression   
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    //4 bytes -- ImageSize  (compressed) It is valid to set this =0 
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    //XpixelsPerM 4 bytes horizontal resolution: Pixels/meter 
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    //YpixelsPerM 4 bytes vertical resolution: Pixels/meter 
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    //ColorsUsed 4 bytes Number of actually used colors =256
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    //ColorsImportant 4 bytes Number of important colors  0 = all 
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
    p++;
    *p = 0;
}
