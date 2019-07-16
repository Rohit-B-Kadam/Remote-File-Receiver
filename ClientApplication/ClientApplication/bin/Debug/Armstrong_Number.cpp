/*Program to check whether given number is Armstrong number or not. ihi

Those numbers which sum of the cube of its digits is equal to that number are known as Armstrong numbers.
For example 153
(1^3) + (5^3) + (3^3) = 1+ 125 + 9 =153
Other Armstrong numbers are : 370,371,407 etc.

General�Definition�:
Those numbers which sum of its digits to power of number of ots digits is equal to that number are known as Armstrong numbers.

Example : 1634
Total digits in 1634 is 4
And (1^4) + (6^4) + (3^4) + (4^4) = 1 + 1296 + 81 + 64 =1634
*/

#include<stdio.h>

int main()
{
	int num,r,sum=0,temp;

	printf("Enter a number: ");
	scanf("%d",&num);

	for(temp = num; num!= 0; num = num/10)
	{
		r = num % 10;
		sum=sum + ( r * r * r);
	}

	if (sum == temp)
	{
		printf("%d is an Armstrong number\n",temp);
	}
	else
	{
		printf("%d is not an Armstrong number\n",temp);
	}

	return 0;
}
