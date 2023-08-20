#include "SimpleUSB.h"

int USB_Init()
{
	return libusb_init(NULL);
}

void USB_Exit()
{
	libusb_exit(NULL);
}

int USB_Get_Device_List()
{
    unsigned char string[256];
    libusb_device_handle* handle = nullptr;
    int ret = 0;
    libusb_device** devs = nullptr;
    ssize_t count = libusb_get_device_list(nullptr, &devs);
    if (count < 0) {
        fprintf(stderr, "fail to get device list: %d\n", count);
        return -1;
    }
    libusb_device* dev = nullptr;
    int i = 0;
    while ((dev = devs[i++]) != nullptr)
    {
        struct libusb_device_descriptor desc;
        ret = libusb_get_device_descriptor(dev, &desc);
        if (ret < 0) {
            fprintf(stderr, "fail to get device descriptor: %d\n", ret);
            return -1;
        }
        fprintf(stdout, "%04x:%04x (bus: %d, device: %d) ",
            desc.idVendor, desc.idProduct, libusb_get_bus_number(dev), libusb_get_device_address(dev));
        uint8_t path[8];
        ret = libusb_get_port_numbers(dev, path, sizeof(path));
        if (ret > 0) {
            fprintf(stdout, "path: %d", path[0]);
            for (int j = 1; j < ret; ++j)
                fprintf(stdout, ".%d", path[j]);
        }
        if (!handle) ret = libusb_open(dev, &handle);
        if (handle)
        {
            if (desc.iManufacturer)
            {
                ret = libusb_get_string_descriptor_ascii(handle, desc.iManufacturer, string, sizeof(string));
                if (ret > 0)
                    printf("\nManufacturer:              %s", (char*)string);
            }
            if (desc.iProduct) {
                ret = libusb_get_string_descriptor_ascii(handle, desc.iProduct, string, sizeof(string));
                if (ret > 0)
                    printf("\nProduct:                   %s", (char*)string);
            }
        }
        if (handle) libusb_close(handle);
        handle = nullptr;
        fprintf(stdout, "\n");
    }
    libusb_free_device_list(devs, 1);
    return 0;
}

void USB_Scan_Device()
{
    int16_t dev_num;
    libusb_device** devs;
    dev_num = libusb_get_device_list(NULL, &devs);
    if (dev_num < 0)
    {
        printf("libusb_get_device_list() error!");
        goto getlist_err;
    }

    USB_Dev_Scan_A_Print(dev_num, devs);

getlist_err:
    libusb_free_device_list(devs, 1);
}

void* USB_Open(uint16_t VID, uint16_t PID)
{
    USB_MSD_ST* pMsd = new USB_MSD_ST;
    *pMsd = { NULL, NULL, 0x00, 0x00 };
    uint8_t err = USB_MSD_Open(VID, PID, pMsd);
    if (err > 0) return NULL;
    else return pMsd;
}

int USB_Write(void* pMsd, uint8_t* buff, int len, uint32_t ms)
{
    int actual_Tx_size = 0;
    uint8_t err = USB_MSD_Bulk_Write((USB_MSD_ST*)pMsd, buff, len, &actual_Tx_size, ms);
    if (err < 0) return err;
    else return actual_Tx_size;
}

int USB_Read(void* pMsd, uint8_t* buff, int len, uint32_t ms)
{
    int actual_Rx_size = 0;
    uint8_t err = USB_MSD_Bulk_Read((USB_MSD_ST*)pMsd, buff, len, &actual_Rx_size, ms);
    if (err < 0) return err;
    else return actual_Rx_size;
}

void USB_Close(void* pMsd)
{
    USB_MSD_Close((USB_MSD_ST*)pMsd);
    delete (USB_MSD_ST*)pMsd;
}