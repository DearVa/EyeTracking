#pragma once
#include <thread>
#include "ref\CyAPI.h"


class CameraCapture
{
public:
    CameraCapture(HANDLE handle,void(*handler)(PUCHAR,LONG));
    CCyUSBDevice* device;
    CCyControlEndPoint* end_point = nullptr;
    CCyUSBEndPoint* in_end_point = nullptr;
    std::thread* thread = nullptr;
    void(*handler)(PUCHAR,LONG);
    unsigned long raw_index = 0;
    unsigned char raw_row = 0;
    bool looping = false;
    void start();
    void stop();
    void abort_xfer_loop(int pending, bool opt, const PUCHAR* buffers, const PUCHAR* contexts, OVERLAPPED in_ov_lap[]);

private:
    unsigned char * header;
    static void create_header(unsigned char* header, int height, int width);
};
