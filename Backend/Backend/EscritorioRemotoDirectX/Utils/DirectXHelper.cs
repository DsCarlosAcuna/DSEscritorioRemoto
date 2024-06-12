using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace EscritorioRemotoDirectX.Utils
{
    public static class DirectXHelper
    {
        public static void InitializeDirectX(out SharpDX.Direct3D11.Device device, out OutputDuplication outputDuplication)
        {
            var factory = new Factory1();
            var adapter = factory.GetAdapter1(0);
            var output = adapter.GetOutput(0);
            var output1 = output.QueryInterface<Output1>();

            device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            outputDuplication = output1.DuplicateOutput(device);

            output1.Dispose();
            output.Dispose();
            adapter.Dispose();
            factory.Dispose();
        }
    }
}
