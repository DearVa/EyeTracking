using OpenCvSharp;
using Sdcb.FFmpeg.Codecs;
using Sdcb.FFmpeg.Formats;
using Sdcb.FFmpeg.Raw;
using Sdcb.FFmpeg.Utils;

namespace EyeTracking;

public class VideoDecoder : IDisposable
{
    private readonly FormatContext format;
    private readonly CodecContext  context;

    public VideoDecoder(string file)
    {
        format = FormatContext.OpenInputUrl(file);
        var   stream = format.GetVideoStream();
        var   param  = stream.Codecpar!;
        var codec  = Codec.FindDecoderById(param.CodecId);
        context = new CodecContext(codec);
        context.FillParameters(param);
        context.Open(codec);
    }

    public IEnumerable<Mat> Decode()
    {
        using var packet = new Packet();
        using var frame  = new Frame();
        while (true)
        {
            switch (format.ReadFrame(packet))
            {
                case CodecResult.EOF:
                default:
                    goto final;
                case CodecResult.Again:
                    packet.Unref();
                    continue;
                case CodecResult.Success:
                    break;
            }


            SendPacket(packet);

            while (true)
            {
                switch (context.ReceiveFrame(frame))
                {
                    case CodecResult.Again:
                    default:
                        goto outer;
                    case CodecResult.EOF:
                        goto final;
                    case CodecResult.Success:
                    {
                        yield return Mat.FromPixelData(context.Height, context.Width, MatType.CV_8UC1,
                            frame.ToImageBuffer());
                        break;
                    }
                }


            }

            outer:
            continue;
        }

        final:
        yield break;
    }

    public void Dispose()
    {
        format.Dispose();
        context.Dispose();
    }

    private void SendPacket(Packet packet)
    {
        unsafe
        {
            ffmpeg.avcodec_send_packet(context, packet);
        }
    }
}