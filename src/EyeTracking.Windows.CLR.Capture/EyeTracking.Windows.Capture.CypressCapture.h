#pragma once
#include "CyApi.h"

namespace EyeTracking
{
    namespace Windows
    {
        namespace Capture
        {
            public ref class CypressCapture sealed
            {
            public:
                delegate void Handler(PUCHAR buffer,LONG length);
                CypressCapture(System::IntPtr^ handle);
                void Start(Handler^ receiver);
                void Stop();

            private:
                CCyUSBDevice* device;
                CCyControlEndPoint* end_point;
                CCyUSBEndPoint* in_end_point;
                Handler^ handler;
                unsigned char* header;
                unsigned long raw_index = 0;
                unsigned char raw_row = 0;
                bool looping = false;
                void abort_xfer_loop(int pending, bool opt, const PUCHAR* buffers, const PUCHAR* contexts, OVERLAPPED in_ov_lap[]);
                static void create_header(unsigned char* header, int height, int width);
                void loop();
            };
        }
    }
}
