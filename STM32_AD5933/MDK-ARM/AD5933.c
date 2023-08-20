#include "AD5933.h"
#include "math.h"
#include "i2c.h"
#include "usbd_cdc_if.h"
#include "usbd_desc.h"

extern I2C_HandleTypeDef hi2c2;
long ReadTemp,realArr[3],imageArr[3];

//Which i2c to use
#define I2C_USED hi2c2

void Write_Byte(char nAddr,uint8_t nValue)//nAddr��д���ֽ�nValue
{   
	uint16_t nTemp = 0x1A;      // AD5933��Ĭ�ϵ�ַ&д����λ���ͣ�
	uint8_t data[2];
	data[0] = nAddr;
	data[1] = nValue;
	int code = HAL_I2C_Master_Transmit(&I2C_USED, nTemp, data, 2, 500);
  return;
}

void SetPointer(char nAddr)  //   ���õ�ַָ��
{          
	uint16_t nTemp = 0x1A;      // AD5933��Ĭ�ϵ�ַ&д����λ���ͣ�
	uint8_t data[2];
	data[0] = 0xB0;		// ����ָ������1101 0000
	data[1] = nAddr;
	HAL_I2C_Master_Transmit(&I2C_USED, nTemp, data, 2, 500);
  return;
}

uint8_t Rece_Byte(char nAddr)//��ȡnAddr�е��ֽڵ�����ֵ
{   
	uint16_t nTemp = 0x1B;
	uint8_t data;
	SetPointer(nAddr);
	HAL_I2C_Master_Receive(&I2C_USED, nTemp, &data, 1, 500);
	return data;
}

uint16_t AD5933_Tempter(void)
{
	 uint16_t Tm;        //���棬�¶�
   
   Write_Byte(0x80,0x90);  //�����¶Ȳ���
   Tm=Rece_Byte(0x92);     //�����¶ȣ�������Tm��
   Tm<<=8;
   Tm+=Rece_Byte(0x93);
	 return Tm;
}


void Fre_To_Hex(float fre,uint8_t *buf)
{
	uint32_t dat;
	dat=(536870912/(double)(AD5933_MCLK*1000000))*fre;  
	buf[0]=dat>>16;
	buf[1]=dat>>8;
	buf[2]=dat;
}

/*Fre_Begin��ʼƵ�ʣ�Fre_UPƵ��������UP_Num��������OUTPUT_Vatage�����ѹ��Gain����ϵ����SWeep_RepɨƵΪ1�ظ�Ϊ0*/
/*
Fre_Begin 		��ʼƵ�� ��HZ��
Fre_UP				����Ƶ�ʣ�HZ��
UP_Num				��������
OUTPUT_Vatage	�����ѹ
								AD5933_OUTPUT_2V
								AD5933_OUTPUT_1V
								AD5933_OUTPUT_400mV
								AD5933_OUTPUT_200mV
								
Gain					PGA����			
							AD5933_Gain_1
							AD5933_Gain_5

SWeep_Rep			ɨ��ģʽ
							AD5933_Fre_UP 	����Ƶ��
							AD5933_Fre_Rep	�ظ�Ƶ��
*/

int AD5933_Sweep (float Fre_Begin,float Fre_UP,uint16_t UP_Num,uint16_t OUTPUT_Vatage,uint16_t Gain,uint16_t SWeep_Rep)
{
	uint8_t SValue[3], IValue[3], NValue[2], CValue[2];
	uint16_t buf=0;

	Fre_To_Hex(Fre_Begin,SValue);
	Fre_To_Hex(Fre_UP,IValue);
	
	NValue[0]=UP_Num>>8;
	NValue[1]=UP_Num;
	
#ifdef AD5933_MCLK_USE_OUT
	buf=OUTPUT_Vatage|Gain|SWeep_Rep|AD5933_OUT_MCLK;
#else
	buf=OUTPUT_Vatage|Gain|SWeep_Rep|AD5933_IN_MCLK;
#endif
	
	CValue[0]=buf>>8;
	CValue[1]=buf;
	
	Scale_imp(SValue,IValue,NValue,CValue);
	
	return 0;
}

/*SValue[3]��ʼƵ�ʣ�IValue[3]Ƶ��������NValue[2]��������CValue[2]�����֣�ki����ϵ����PsɨƵΪ1�ظ�Ϊ0*/

void Scale_imp (uint8_t *SValue,uint8_t *IValue,uint8_t *NValue,uint8_t *CValue)
{
		int i,AddrTemp;
	
		uint8_t SWeep_Rep=((CValue[0]&0xF0)==(AD5933_Fre_UP>>8))?1:0;
	
		uint8_t Mode=CValue[0]&0x0f;

		AddrTemp=0X82; //��ʼ����ʼƵ�ʼĴ���
		for(i = 0;i <3;i++)
		{
				Write_Byte(AddrTemp,SValue[i]);
				AddrTemp++;
		}     
		
		AddrTemp=0X85; //��ʼ��Ƶ�������Ĵ���
		for(i = 0;i <3;i++)
		{
				Write_Byte(AddrTemp,IValue[i]);
				AddrTemp++;
		} 
		
		AddrTemp=0X88; //��ʼ��Ƶ�ʵ����Ĵ���
		for(i = 0;i <2;i++)
	 {
				Write_Byte(AddrTemp,NValue[i]);
				AddrTemp++;
	 } 
	 
	 //��ʼ�����ƼĴ�����1011 0001 0000 0000����ģʽ��2V��һ���Ŵ��ڲ�ʱ��
	 
		AddrTemp=0X80; 
		Write_Byte(AddrTemp,Mode|(AD5933_Standby>>8));
	 
		AddrTemp++;
		Write_Byte(AddrTemp,CValue[1]);
	 
		AddrTemp++;
		Write_Byte(0x80,Mode|(AD5933_SYS_Init>>8));//���ƼĴ���д���ʼ��Ƶ��ɨ������
		
	  HAL_Delay(10);
		Write_Byte(0X80,Mode|(AD5933_Begin_Fre_Scan>>8));//���ƼĴ���д�뿪ʼƵ��ɨ������
	 
		while(1)
	  { 
			while(1)
			{
				 ReadTemp=Rece_Byte(0x8F);  //��ȡ״̬�Ĵ������DFT�Ƿ����
				if (ReadTemp&0x02)
				 break; 
			}  
		
			realArr[0]=Rece_Byte(0x94);
			realArr[1]=Rece_Byte(0x95);
			realArr[2]=(realArr[0]<<8)+realArr[1];
		
			imageArr[0]=Rece_Byte(0x96);
			imageArr[1]=Rece_Byte(0x97);
			imageArr[2]=(imageArr[0]<<8)+imageArr[1]; 
		
			
			ReadTemp=Rece_Byte(0x8F);  //��ȡ״̬�Ĵ������Ƶ��ɨ���Ƿ����
			
			if (ReadTemp&0x04)
			break;
			
			if (SWeep_Rep==1)
			Write_Byte(0X80,CValue[0]);	//���ƼĴ���д������Ƶ�ʣ�������һ��Ƶ�ʵ�)������
			
			else
			Write_Byte(0X80,CValue[0]);	//���ƼĴ���д���ظ���ǰƵ�ʵ�ɨ��	
	  }  
		
			return;
}

