<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\mscorlib.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Drawing.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Extensions.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Windows.Forms.dll</Reference>
  <Namespace>System</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Configuration</Namespace>
  <Namespace>System.Drawing.Design</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Drawing.Printing</Namespace>
  <Namespace>System.Drawing.Text</Namespace>
  <Namespace>System.Windows.Forms</Namespace>
  <Namespace>System.Windows.Forms.ComponentModel.Com2Interop</Namespace>
  <Namespace>System.Windows.Forms.Design</Namespace>
  <Namespace>System.Windows.Forms.Layout</Namespace>
  <Namespace>System.Windows.Forms.PropertyGridInternal</Namespace>
  <Namespace>System.Windows.Forms.VisualStyles</Namespace>
</Query>

void Main()
{
	//test class
	Element myNewElement = new Element(ay_:-320, az_:-52, AIsConn:false, by_:-20, bz_:358, BIsConn:true, t:15, steel_fy:355.0);
	
	//myNewElement.CheckInput();
	myNewElement.CalcEffWidth(-355,-23);
	Console.WriteLine ("Results: coordiantes at global A, A1, B1, B");
	Console.WriteLine ("Point A: Y= " + myNewElement.A.Y + "; Z= " + myNewElement.A.Z);
	Console.WriteLine ("Point A1: Y= " + myNewElement.A1.Y + "; Z= " + myNewElement.A1.Z);
	Console.WriteLine ("Point B1: Y= " + myNewElement.B1.Y + "; Z= " + myNewElement.B1.Z);
	Console.WriteLine ("Point B: Y= " + myNewElement.B.Y + "; Z= " + myNewElement.B.Z);
	
	//graphics
	//Application.Run(new MyForm(myNewElement.GlobA.Y,myNewElement.GlobA.Z,myNewElement.GlobB.Y,myNewElement.GlobB.Z));
	Application.Run(new MyForm(myNewElement));
}

public class Element
{
	/////properies
	//points of an element Y,Z coordinates and 
	public struct point	{public double Y,Z;	public bool isConnected;}
	public struct distribution {public bool tension, compression, bending;}
	
	//set up default thickness
	public double thickness = 0;
	public double fy;
	//create end points A, B in global coords
	public point GlobA = new point();
	public point GlobB = new point();
	
	//create end points A, B  - local
	public point A = new point();
	public point B = new point();
	//crate intermediate point defines effective parts - local
	public point A1 = new point();
	public point B1 = new point();
	
	distribution ElementStressDistribution = new distribution();
	
	/////constructor
	public Element(double ay_, double az_, bool AIsConn, double by_, double bz_, bool BIsConn, double t, double steel_fy)  
	{

		//global coords
		GlobA.Y=ay_;
		GlobA.Z=az_;
		GlobB.Y=by_;
		GlobB.Z=bz_;
		
		//checking
		Console.WriteLine ("Global coordinates");
		Console.WriteLine ("GlobA.Y: "+GlobA.Y);
		Console.WriteLine ("GlobA.Z: "+GlobA.Z);
		Console.WriteLine ("GlobB.Y: "+GlobB.Y);
		Console.WriteLine ("GlobB.Y: "+GlobB.Z);

		//transform to local
		//send A, B the coordinates to function transform to transform to local
		double [] temp1 = new double [2];
		temp1=transform(x1:GlobA.Y,y1:GlobA.Z,x2:GlobB.Y,y2:GlobB.Z,input_Y_coord:GlobA.Y,input_Z_coord:GlobA.Z,toLocal:true);
		A.Y=Math.Round(temp1[0],3);
		A.Z=Math.Round(temp1[1],3);
		temp1=transform(x1:GlobA.Y,y1:GlobA.Z,x2:GlobB.Y,y2:GlobB.Z,input_Y_coord:GlobB.Y,input_Z_coord:GlobB.Z,toLocal:true);
		B.Y=Math.Round(temp1[0],3);
		B.Z=Math.Round(temp1[1],3);
		
		//checking
		Console.WriteLine ("After converting to local");
		Console.WriteLine ("A.Y: "+A.Y);
		Console.WriteLine ("A.Z: "+A.Z);
		Console.WriteLine ("B.Y: "+B.Y);
		Console.WriteLine ("B.Y: "+B.Z);
		Console.WriteLine ("test dlzka: " + Math.Sqrt((ay_-by_)*(ay_-by_)+(az_-bz_)*(az_-bz_)));
		
		
		// intermediate points what will be calculated later
		A1.Y=0;
		A1.Z=0; 
		B1.Y=0;
		B1.Z=0; 		
		
		//other properties
		A.isConnected=AIsConn;
		B.isConnected=BIsConn;
		thickness = t;
		fy=steel_fy;
		//set up default Element stress distribution status
		ElementStressDistribution.tension = false;
		ElementStressDistribution.compression = false;
		ElementStressDistribution.bending = false;
		
	}
	
	//method for calcualtin effective parts of elements
	//this method defines undefined coordinates A1 and B1 and may modify B if the member is a flange
	public void CalcEffWidth(double sigmaA, double sigmaB)
	{
		double psi=0;
		double ksigma=0;
		double lambdaP=0;
		double rho=0;
		double beff=0;
		double bc=0;
		double bt=0;
		double length = Math.Sqrt((A.Y-B.Y)*(A.Y-B.Y)+(A.Z-B.Z)*(A.Z-B.Z));
		
		////////////////////////////////////
		//internal compression elements - equations from EN 1993-1-5 TABLE 4.1
		////////////////////////////////////
		
		if (A.isConnected == true && B.isConnected == true)
		{
			Console.WriteLine ("Element is internal");
			//check stress distribution			
			//tension or no stress
			if (sigmaA>=0 && sigmaB>=0)
			{			
			ElementStressDistribution.tension=true;
			ElementStressDistribution.compression=false;
			ElementStressDistribution.bending=false;
			}
			
			//compression constatnt value
			else if (sigmaA<0 && (sigmaA==sigmaB))
			{
			ElementStressDistribution.tension=false;
			ElementStressDistribution.compression=true;
			ElementStressDistribution.bending=false;
			psi=1;
			ksigma=CalcKsigmaInternalElement(psi);
			lambdaP=CalcLambdaP(ksigma,length,thickness);
			rho=CalcRhoInternalElement(lambdaP,psi);
			A1.Y = A.Y + rho*length/2;
			B1.Y = B.Y - rho*length/2;
			}
			
			//compression linear distribution, at A the value is greater tha at B
			else if (sigmaA<0 && sigmaB<0 && sigmaA<sigmaB)   //sigmaA < sigmaB because these values are negatives
			{	
				psi=sigmaB/sigmaA;
				if (psi>0 && psi<1)
				{
				ElementStressDistribution.tension=false;
				ElementStressDistribution.compression=true;
				ElementStressDistribution.bending=false;
				ksigma=CalcKsigmaInternalElement(psi);
				lambdaP=CalcLambdaP(ksigma,length,thickness);
				rho=CalcRhoInternalElement(lambdaP,psi);
				beff=rho*length;
				A1.Y = A.Y + beff*2.0/(5.0-psi);
				B1.Y = B.Y - (beff-(beff*2.0/(5.0-psi)));
				}
				else
				{
				Console.WriteLine ("Internal element calculation, sigmaA<0, sigmaB<0, sigmaA<sigmaB, psi is out of range ");
				}
			}
			
			//compression linear distribution, at B the value is greater tha at A, 
			else if (sigmaA<0 && sigmaB<0 && sigmaA>sigmaB) 
			{	
				psi=sigmaA/sigmaB;
				if (psi>0 && psi<1)
				{
				ElementStressDistribution.tension=false;
				ElementStressDistribution.compression=true;
				ElementStressDistribution.bending=false;
				ksigma=CalcKsigmaInternalElement(psi);
				lambdaP=CalcLambdaP(ksigma,length,thickness);
				rho=CalcRhoInternalElement(lambdaP,psi);
				beff=rho*length;
				A1.Y = A.Y + (beff-(beff*2.0/(5.0-psi)));
				B1.Y = B.Y - beff*2.0/(5.0-psi);
				}
				else
				{
				Console.WriteLine ("Internal element calculation, sigmaA<0, sigmaB<0, sigmaA>sigmaB, psi is out of range ");
				}	
			}
			
			//bending, compression at point A tension at point B, linear distribution
			else if (sigmaA<=0 && sigmaB>=0)
			{	
				// manage dividing by zero - change value 0 to 0.01
				if (sigmaA==0.0)
				{
				sigmaA=-0.01;
				}
				// manage dividing by zero - change value 0 to 0.01
				if (sigmaB==0.0)
				{
				sigmaB=0.01;
				}
				
				psi=sigmaB/sigmaA;
				if (psi<0)
				{
				ElementStressDistribution.tension=false;
				ElementStressDistribution.compression=false;
				ElementStressDistribution.bending=true;
				ksigma=CalcKsigmaInternalElement(psi);
				lambdaP=CalcLambdaP(ksigma,length,thickness);
				rho=CalcRhoInternalElement(lambdaP,psi);
				//bc=length/(1-psi);
				bt=(length/(Math.Abs(sigmaA)+Math.Abs(sigmaB)))*Math.Abs(sigmaB);
				bc=length-bt;
				beff=rho*bc;
				A1.Y = A.Y + beff*0.4;
				B1.Y = A.Y + bc - beff*0.6;
				}
				else
				{
				Console.WriteLine ("Internal element calculation, sigmaA<0, sigmaB>0, psi is out of range ");
				}
			}
			
			//bending tension at point A compression at point B, linear distribution
			else if (sigmaA>=0 && sigmaB<=0)
			{
				psi=sigmaA/sigmaB;
				if (psi<0)
				{
				ElementStressDistribution.tension=false;
				ElementStressDistribution.compression=false;
				ElementStressDistribution.bending=true;
				ksigma=CalcKsigmaInternalElement(psi);
				lambdaP=CalcLambdaP(ksigma,length,thickness);
				rho=CalcRhoInternalElement(lambdaP,psi);
				bc=length/(1-psi);
				beff=rho*bc;
				A1.Y = B.Y - bc + beff*0.6;
				B1.Y = B.Y - beff*0.4;
				}
				else
				{
				Console.WriteLine ("Internal element calculation, sigmaA>0, sigmaB<0, psi is out of range ");
				}
			}
			
			else
			{
			Console.WriteLine ("Stress distribution can't recognised!!");
			}
			
			//output for check
			Console.WriteLine ("Output for check START");
			Console.WriteLine ("length: " +length);
			Console.WriteLine ("psi:" + psi);
			Console.WriteLine ("ksigma: " + ksigma);
			Console.WriteLine ("lambdaP: " + lambdaP);
			Console.WriteLine ("bt: " + bt);
			Console.WriteLine ("beff: " + beff);
			Console.WriteLine ("bc: " + bc);
			Console.WriteLine ("rho: " + rho);
			Console.WriteLine ("AY: " + A.Y);
			Console.WriteLine ("A1Y: " + A1.Y);
			Console.WriteLine ("B1Y: " + B1.Y);
			Console.WriteLine ("BY: " + B.Y);
			Console.WriteLine ("Output for check END");
			Console.WriteLine ("ElementStressDistribution.tension= " + ElementStressDistribution.tension);
			Console.WriteLine ("ElementStressDistribution.compression= " + ElementStressDistribution.compression);
			Console.WriteLine ("ElementStressDistribution.bending= " + ElementStressDistribution.bending);
		}
		
		
		
		
		
		////////////////////////////////////
		// outstanding compression elements - equations from EN 1993-1-5 TABLE 4.2
		///////////////////////////////////
			
		
		else if (A.isConnected == true && B.isConnected == false)  //connected at and A
		{

			Console.WriteLine ("Element is outstanding, connected at point A");
			//check stress distribution
			//tension or no stress
			if (sigmaA>=0 && sigmaB>=0)
			{			
			ElementStressDistribution.tension=true;
			ElementStressDistribution.compression=false;
			ElementStressDistribution.bending=false;
			}
			
			//compression linear distribution, at A the negative stress is smaler than at B
			else if (sigmaA<=0 && sigmaB<=0 && sigmaA>=sigmaB  ) //sigmaA > sigmaB because these are negative values
			{													///>= added against EN due to calc psi=1
				psi=sigmaA/sigmaB;
				if (psi<=1 && psi>=0)
				{
					// managing dividing by zero - change value 0 to 0.01
					if (sigmaA==0.0)
					{
						sigmaA=-0.01;
					}
					// manage dividing by zero - change value 0 to 0.01
					if (sigmaB==0.0)
					{
						sigmaB=-0.01;
					}
				
					ElementStressDistribution.tension=false;
					ElementStressDistribution.compression=true;
					ElementStressDistribution.bending=false;
					ksigma=CalcKsigmaOutstandingElement_1(psi);
					lambdaP=CalcLambdaP(ksigma,length,thickness);
					rho=CalcRhoOutstandingElement(lambdaP,psi);
					beff=rho*length;
					A1.Y = A.Y + beff;
					B1.Y = double.NaN;
					B.Y = double.NaN;
				}
				else
				{
					Console.WriteLine ("Outstanding element calculation, sigmaA<0, sigmaB<0,sigmaA>=sigmaB, psi is out of range ");
				}
			}
			//compression linear distribution, at A tenson at B compression
			else if (sigmaA>0 && sigmaB<0 ) //sigmaA < sigmaB because these are negative values
			{
				psi=sigmaA/sigmaB;
				if (psi<0)
				{	
					ElementStressDistribution.tension=false;
					ElementStressDistribution.compression=false;
					ElementStressDistribution.bending=true;
					ksigma=CalcKsigmaOutstandingElement_1(psi);
					lambdaP=CalcLambdaP(ksigma,length,thickness);
					rho=CalcRhoOutstandingElement(lambdaP,psi);
					bt=(length/(Math.Abs(sigmaA)+Math.Abs(sigmaB)))*Math.Abs(sigmaA);
					bc=length-bt;
					beff=rho*bc;
					A1.Y = A.Y + bt + beff;
					B1.Y = double.NaN;
					B.Y = double.NaN;
				}
				else
				{
					Console.WriteLine ("Outstanding element calculation, sigmaA>0, sigmaB<0, psi is out of range ");
				}
			}
			
			//compression linear distribution, at A the negative stress is higher than at B
			else if (sigmaA<=0 && sigmaB<=0 && sigmaA<=sigmaB  ) //sigmaA < sigmaB because these are negative values
			{													///<= added against EN due to calc psi=1
				psi=sigmaB/sigmaA;
				if (psi<=1 && psi>=0)
				{
					// managing dividing by zero - change value 0 to 0.01
					if (sigmaA==0.0)
					{
						sigmaA=-0.01;
					}
					// manage dividing by zero - change value 0 to 0.01
					if (sigmaB==0.0)
					{
						sigmaB=-0.01;
					}
					ElementStressDistribution.tension=false;
					ElementStressDistribution.compression=true;
					ElementStressDistribution.bending=false;
					ksigma=CalcKsigmaOutstandingElement_2(psi);
					lambdaP=CalcLambdaP(ksigma,length,thickness);
					rho=CalcRhoOutstandingElement(lambdaP,psi);
					beff=rho*length;
					A1.Y = A.Y + beff;
					B1.Y = double.NaN;
					B.Y = double.NaN;
				}
				else
				{
					Console.WriteLine ("Outstanding element calculation, sigmaA<0, sigmaB<0,sigmaA<=sigmaB, psi is out of range ");
				}
			}
			
			//compression linear distribution, at A compression at B tension
			else if (sigmaA<0 && sigmaB>0) //
			{
				psi=sigmaB/sigmaA;
				if (psi<0)
				{	
					ElementStressDistribution.tension=false;
					ElementStressDistribution.compression=false;
					ElementStressDistribution.bending=true;
					ksigma=CalcKsigmaOutstandingElement_2(psi);
					lambdaP=CalcLambdaP(ksigma,length,thickness);
					rho=CalcRhoOutstandingElement(lambdaP,psi);
					bt=(length/(Math.Abs(sigmaA)+Math.Abs(sigmaB)))*Math.Abs(sigmaB);
					bc=length-bt;
					beff=rho*bc;
					A1.Y = A.Y+beff;
					B1.Y = bc;
				}
				else
				{
					Console.WriteLine ("Outstanding element calculation, sigmaA<0, sigmaB>0,psi is out of range ");
				}
			}

			//output for check
			Console.WriteLine ("Output for check START");
			Console.WriteLine ("length: " +length);
			Console.WriteLine ("psi:" + psi);
			Console.WriteLine ("ksigma: " + ksigma);
			Console.WriteLine ("lambdaP: " + lambdaP);
			Console.WriteLine ("beff: " + beff);
			Console.WriteLine ("bc: " + bc);
			Console.WriteLine ("bt: " + bt);
			Console.WriteLine ("rho: " + rho);
			Console.WriteLine ("AY: " + A.Y);
			Console.WriteLine ("A1Y: " + A1.Y);
			Console.WriteLine ("B1Y: " + B1.Y);
			Console.WriteLine ("BY: " + B.Y);
			Console.WriteLine ("Output for check END");
			Console.WriteLine ("ElementStressDistribution.tension= " + ElementStressDistribution.tension);
			Console.WriteLine ("ElementStressDistribution.compression= " + ElementStressDistribution.compression);
			Console.WriteLine ("ElementStressDistribution.bending= " + ElementStressDistribution.bending);

		}
		
		
		// outstanding compression elements - flange Bconn
		
		else if (A.isConnected == false && B.isConnected == true)  //connected at and B
		{
			Console.WriteLine ("Element is outstanding, connected at point B ");
			
			//check stress distribution
			//tension or no stress
			if (sigmaA>=0 && sigmaB>=0)
			{			
			ElementStressDistribution.tension=true;
			ElementStressDistribution.compression=false;
			ElementStressDistribution.bending=false;
			}
			
			//compression linear distribution, at A the negative stress is higher than at B
			else if (sigmaA<=0 && sigmaB<=0 && sigmaA<=sigmaB  ) //sigmaA < sigmaB because these are negative values
			{													///>= added against EN due to calc psi=1
				psi=sigmaB/sigmaA;
				if (psi<=1 && psi>=0)
				{
					// managing dividing by zero - change value 0 to 0.01
					if (sigmaA==0.0)
					{
						sigmaA=-0.01;
					}
					// manage dividing by zero - change value 0 to 0.01
					if (sigmaB==0.0)
					{
						sigmaB=-0.01;
					}
				
					ElementStressDistribution.tension=false;
					ElementStressDistribution.compression=true;
					ElementStressDistribution.bending=false;
					ksigma=CalcKsigmaOutstandingElement_1(psi);
					lambdaP=CalcLambdaP(ksigma,length,thickness);
					rho=CalcRhoOutstandingElement(lambdaP,psi);
					beff=rho*length;
					A.Y = double.NaN;
					A1.Y = double.NaN;
					B1.Y = length-beff;
					
				}
				else
				{
					Console.WriteLine ("Outstanding element calculation, sigmaA<0, sigmaB<0,sigmaA<=sigmaB, psi is out of range ");
				}
			}
			//compression linear distribution, at A compression at B tension
			else if (sigmaA<0 && sigmaB>0 ) 
			{
				psi=sigmaB/sigmaA;
				if (psi<0)
				{	
					ElementStressDistribution.tension=false;
					ElementStressDistribution.compression=false;
					ElementStressDistribution.bending=true;
					ksigma=CalcKsigmaOutstandingElement_1(psi);
					lambdaP=CalcLambdaP(ksigma,length,thickness);
					rho=CalcRhoOutstandingElement(lambdaP,psi);
					bt=(length/(Math.Abs(sigmaA)+Math.Abs(sigmaB)))*Math.Abs(sigmaB);
					bc=length-bt;
					beff=rho*bc;
					A.Y=double.NaN;
					A1.Y = double.NaN;
					B1.Y = length-bt-beff;
					
				}
				else
				{
					Console.WriteLine ("Outstanding element calculation, sigmaA<0, sigmaB>0, psi is out of range ");
				}
			}
			
			//compression linear distribution, at A the negative stress is lower than at B
			else if (sigmaA<=0 && sigmaB<=0 && sigmaA>=sigmaB  ) //sigmaA > sigmaB because these are negative values
			{													///<= added against EN due to calc psi=1
				psi=sigmaA/sigmaB;
				if (psi<=1 && psi>=0)
				{
					// managing dividing by zero - change value 0 to 0.01
					if (sigmaA==0.0)
					{
						sigmaA=-0.01;
					}
					// manage dividing by zero - change value 0 to 0.01
					if (sigmaB==0.0)
					{
						sigmaB=-0.01;
					}
					ElementStressDistribution.tension=false;
					ElementStressDistribution.compression=true;
					ElementStressDistribution.bending=false;
					ksigma=CalcKsigmaOutstandingElement_2(psi);
					lambdaP=CalcLambdaP(ksigma,length,thickness);
					rho=CalcRhoOutstandingElement(lambdaP,psi);
					beff=rho*length;
					A.Y=double.NaN;
					A1.Y = double.NaN;
					B1.Y = length-beff;
					
				}
				else
				{
					Console.WriteLine ("Outstanding element calculation, sigmaA<0, sigmaB<0,sigmaA>=sigmaB, psi is out of range ");
				}
			}
			
			//compression linear distribution, at A tension at B compression
			else if (sigmaA>0 && sigmaB<0) 
			{
				psi=sigmaA/sigmaB;
				if (psi<0)
				{	
					ElementStressDistribution.tension=false;
					ElementStressDistribution.compression=false;
					ElementStressDistribution.bending=true;
					ksigma=CalcKsigmaOutstandingElement_2(psi);
					lambdaP=CalcLambdaP(ksigma,length,thickness);
					rho=CalcRhoOutstandingElement(lambdaP,psi);
					bt=(length/(Math.Abs(sigmaA)+Math.Abs(sigmaB)))*Math.Abs(sigmaA);
					bc=length-bt;
					beff=rho*bc;
					A1.Y = bt;
					B1.Y = length-bc;
				}
				else
				{
					Console.WriteLine ("Outstanding element calculation, sigmaA<0, sigmaB>0,psi is out of range ");
				}
			}

			//output for check
			Console.WriteLine ("Output for check START");
			Console.WriteLine ("length: " +length);
			Console.WriteLine ("psi:" + psi);
			Console.WriteLine ("ksigma: " + ksigma);
			Console.WriteLine ("lambdaP: " + lambdaP);
			Console.WriteLine ("beff: " + beff);
			Console.WriteLine ("bc: " + bc);
			Console.WriteLine ("bt: " + bt);
			Console.WriteLine ("rho: " + rho);
			Console.WriteLine ("AY: " + A.Y);
			Console.WriteLine ("A1Y: " + A1.Y);
			Console.WriteLine ("B1Y: " + B1.Y);
			Console.WriteLine ("BY: " + B.Y);
			Console.WriteLine ("Output for check END");
			Console.WriteLine ("ElementStressDistribution.tension= " + ElementStressDistribution.tension);
			Console.WriteLine ("ElementStressDistribution.compression= " + ElementStressDistribution.compression);
			Console.WriteLine ("ElementStressDistribution.bending= " + ElementStressDistribution.bending);			
			
		}
		

		
		// element isnt connected
		
		else	
		{
			Console.WriteLine ("Element isn't connected");
		}
		
	//transform to global
	//temporary array to get coordiantes from function transform
	double [] temp = new double [2];
	
	//send all the coordinates to function transform to transform to global
	temp=transform(x1:GlobA.Y,y1:GlobA.Z,x2:GlobB.Y,y2:GlobB.Z,input_Y_coord:A.Y,input_Z_coord:A.Z,toLocal:false);
	A.Y=Math.Round(temp[0],3);
	A.Z=Math.Round(temp[1],3);
	temp=transform(x1:GlobA.Y,y1:GlobA.Z,x2:GlobB.Y,y2:GlobB.Z,input_Y_coord:A1.Y,input_Z_coord:A1.Z,toLocal:false);
	A1.Y=Math.Round(temp[0],3);
	A1.Z=Math.Round(temp[1],3);
	temp=transform(x1:GlobA.Y,y1:GlobA.Z,x2:GlobB.Y,y2:GlobB.Z,input_Y_coord:B1.Y,input_Z_coord:B1.Z,toLocal:false);
	B1.Y=Math.Round(temp[0],3);
	B1.Z=Math.Round(temp[1],3);
	temp=transform(x1:GlobA.Y,y1:GlobA.Z,x2:GlobB.Y,y2:GlobB.Z,input_Y_coord:B.Y,input_Z_coord:B.Z,toLocal:false);
	B.Y=Math.Round(temp[0],3);
	B.Z=Math.Round(temp[1],3);
	
	//output for check
	Console.WriteLine ("**********END ELEMENT************");
	}
		
	
	/////functions
	
	//function to calculate k sigma for intenal elements accordign to: EN 1993-1-5; table 4.1
	private double CalcKsigmaInternalElement(double psi)
	{
		double ksigma=0;
		if (psi==1.0)
		{
		ksigma=4;
		}
		
		else if (psi<1 && psi>0)
		{
		ksigma=8.2/(1.05+psi);
		}
		
		else if (psi==0)
		{
		ksigma=7.81;
		}
		
		else if (psi<0 && psi>-1)
		{
		ksigma=7.81-6.29*psi+9.78*psi*psi;
		}
		
		else if (psi==-1)
		{
		ksigma=23.9;
		}
		
		else if (psi<-1 && psi>=-3)
		{
		ksigma=5.98*Math.Pow((1-psi),2);
		}
		
		else
		{
		// consider revision that emelemnt is tensioned and fully effective
		Console.WriteLine ("ksigma for internal element wasn't calculated, psi is out of scope, ksigma=0. Can we assume that the bended element is fully effective??");
		}
		
		return ksigma;
	}
	//function to calculate k sigma for outstanding elements accordign to: EN 1993-1-5; table 4.2 - 2nd part
	private double CalcKsigmaOutstandingElement_2(double psi)
	{
		double ksigma=0;
		if (psi==1.0)
		{
		ksigma=0.43;
		}
		
		else if (psi<1 && psi>0)
		{
		ksigma=0.578/(psi+0.34);
		}
		
		else if (psi==0)
		{
		ksigma=1.70;
		}
		
		else if (psi<0 && psi>-1)
		{
		ksigma=1.7-5*psi+17.1*psi*psi;
		}
		
		else if (psi==-1)
		{
		ksigma=23.8;
		}
				
		else
		{
		// consider revision - the element is fully effective
		Console.WriteLine ("ksigma for outstanding (2nd part) element wasn't calculated, psi is out of scope, ksigma=0");
		}
		
		return ksigma;
	}

	//function to calculate k sigma for outstanding elements accordign to: EN 1993-1-5; table 4.2 - 1st part
	private double CalcKsigmaOutstandingElement_1(double psi)
	{
		double ksigma=0;
		if (psi<=1.0 && psi>=-3)
		{
		ksigma=0.57-0.21*psi+0.07*psi*psi;
		}			
		else
		{
		// consider revision - the element is fully effective
		Console.WriteLine ("ksigma for outstanding (1st part) element wasn't calculated, psi is out of scope, ksigma=0");
		}
		
		return ksigma;
	}
	//function to calculate Lambda P accordign to: EN 1993-1-5; eq below(4.3)
	private double CalcLambdaP(double ksigma,double b,double t)
	{
		double lambdaP;
		lambdaP=(b/t)/(28.4*Math.Sqrt(235.0/fy)*Math.Sqrt(ksigma));
		return lambdaP;
		
	}
	//function to calculate rho for internal compression elements accordign to: EN 1993-1-5; eq (4.2)
	private double CalcRhoInternalElement(double lambdaP,double psi)
	{
		double rho=0.0;
		if (lambdaP<=(0.5+Math.Sqrt(0.085-0.055*psi)))
		{
		rho=1.0;
		}
		else
		{
			double temp;
			temp=(lambdaP-0.055*(3.0+psi))/(lambdaP*lambdaP);
			if (temp<1.0)
			{
			rho=temp;
			}
			else
			{
			rho=1;
			}
		}
		return rho;
	}
	//function to calculate rho for outsanding compression elements accordign to: EN 1993-1-5; eq (4.3)
	private double CalcRhoOutstandingElement(double lambdaP,double psi)
	{
		double rho=0.0;
		if (lambdaP<=0.748)
		{
		rho=1.0;
		}
		else
		{
			double temp;
			temp=(lambdaP-0.188)/Math.Pow(lambdaP,2);
			if (temp<1.0)
			{
			rho=temp;
			}
			else
			{
			rho=1.0;
			}
		}
		return rho;
	}
	
	///// checking
	public void CheckInput ()		
	{	
		Console.WriteLine ("Check input START");
		Console.WriteLine (A.Y);
		Console.WriteLine (A.Z);
		Console.WriteLine (A.isConnected);
		Console.WriteLine (B.Y);
		Console.WriteLine (B.Z);
		Console.WriteLine (B.isConnected);
		Console.WriteLine (thickness);
		Console.WriteLine (fy);
		Console.WriteLine ("Check input END");
	}
	
	//transform to global
	public double[] transform (double x1, double y1, double x2, double y2, double input_Y_coord, double input_Z_coord, bool toLocal) 	//according Element object coordinates to x=Y, y=Z
	{	
		const double pi=Math.PI;
		//array what is returned by the function
		double [] GlobCoord = new double [2];
		double [] LocCoord = new double [2];
		
		//variables what define the transforamtion
		double tr_length, movex, movey, rotation;
		
		tr_length=Math.Sqrt((y2-y1)*(y2-y1)+(x2-x1)*(x2-x1));
		movex=-x1;
		movey=-y1;
		
		//cases what can occure at member position
		//the value of member rotation must be follows to make transformtion right
		if (
			((x1<x2) && (y1<y2) && x1>=0 && y1>=0 && x2>0 && y2>0) || 		//1 quadrant case 1 
			((x1<x2) && (y1<y2) && x1<0 && y1<0 && x2<=0 && y2<=0) ||		//3 quadrant case 2
			((x1<x2) && (y1<y2) && x1>=0 && y1<0 && x2>=0 && y2<0) ||		//2 quadrant case 1
			((x1<x2) && (y1<y2) && x1<0 && y1>=0 && x2<=0 && y2>0) || 		//4 quadrant case 4
			((x1<x2) && (y1<y2) && x1>=0 && y1<=0 && x2>0 && y2>0) || 		//from 1 to 2 quadrant case 1
			((x1<x2) && (y1<y2) && x1<0 && y1<0 && x2>=0 && y2<=0) || 		//from 2 to 3 quadrant case 3
			((x1<x2) && (y1<y2) && x1<0 && y1<0 && x2<=0 && y2>=0) || 		//from 3 to 4 quadrant case 1
			((x1<x2) && (y1<y2) && x1<=0 && y1>=0 && x2>0 && y2>0) || 		//from 4 to 1 quadrant case 1
			((x1<x2) && (y1<y2) && x1<=0 && y1<0 && x2>=0 && y2>0) || 		//from 1 to 3 quadrant case 1
			
			((y1<y2) && x1==x2) ||	//horizontal case 1
			((x1<x2) && y1==y2) 	//vertical case 1
			)
		{
			rotation=-(Math.Asin(Math.Abs(y2-y1)/tr_length));		
		}
		else if (
				((x1>x2) && (y1>y2) && x1>0 && y1>0 && x2>=0 && y2>=0) || 	//1 quadrant case 2
				((x1>x2) && (y1>y2) && x1<=0 && y1<=0 && x2<0 && y2<0) ||	//3 quadrant case 1
				((x1>x2) && (y1>y2) && x1>0 && y1<=0 && x2>=0 && y2<0) ||	//2 quadrant case 2
				((x1>x2) && (y1>y2) && x1<=0 && y1>=0 && x2<0 && y2>0) ||	//4 quadrant case 3
				((x1>x2) && (y1>y2) && x1>0 && y1>0 && x2>=0 && y2<=0) ||	//from 1 to 2 quadrant case 2
				((x1>x2) && (y1>y2) && x1>0 && y1>0 && x2<=0 && y2>=0) 		//from 4 to 1 quadrant case 2
				)
		{
			rotation=-(Math.Asin(Math.Abs(y2-y1)/tr_length))+pi;
		}
		else if (
				((x1<x2) && (y1>y2) && x1>=0 && y1>0 && x2>0 && y2>=0) ||	//1 quadrant case 3
				((x1<x2) && (y1>y2) && x1<0 && y1<=0 && x2<=0 && y2<0) ||	//3 quadrant case 4
				((x1<x2) && (y1>y2) && x1>=0 && y1<=0 && x2>0 && y2<0) ||	//2 quadrant case 3
				((x1<x2) && (y1>y2) && x1<0 && y1>=0 && x2<=0 && y2>0) ||	//4 quadrant case 2
				((x1<x2) && (y1>y2) && x1>=0 && y1>=0 && x2>0 && y2>0) ||	//from 1 to 2 quadrant case 4
				((x1<x2) && (y1>y2) && x1<=0 && y1<=0 && x2>0 && y2<0) ||	//from 2 to 3 quadrant case 1
				((x1<x2) && (y1>y2) && x1<0 && y1>0 && x2<=0 && y2<=0) ||	//from 3 to 4 quadrant case 3
				((x1<x2) && (y1>y2) && x1<0 && y1>0 && x2>=0 && y2>=0) ||	//from 4 to 1 quadrant case 3
				((x1<x2) && (y1>y2) && x1<=0 && y1>=0 && x2>=0 && y2<=0)	//from 4 to 2 quadrant case 1
				)
		{
			rotation=(Math.Asin(Math.Abs(y2-y1)/tr_length));
		}	
		else if (
				((x1>x2) && (y1<y2) && x1>0 && y1>=0 && x2>=0 && y2>0) || 	//1 quadrant case 4
				((x1>x2) && (y1<y2) && x1>0 && y1<0 && x2>=0 && y2<=0) ||	//2 quadrant case 4
				((x1>x2) && (y1<y2) && x1>0 && y1<0 && x2>=0 && y2>=0) ||	//from 1 to 2 quadrant case 3
				((x1>x2) && (y1<y2) && x1>=0 && y1>=0 && x2<0 && y2>0) 		//from 4 to 1 quadrant case 4
				)
		{
			rotation=(Math.Asin(Math.Abs(y2-y1)/tr_length))-pi;
		}
		else if (
				((x1>x2) && (y1<y2) && x1<=0 && y1<0 && x2<0 && y2<=0) || 	//3 quadrant case 3
				((x1>x2) && (y1<y2) && x1<=0 && y1>=0 && x2<0 && y2>0) ||	//4 quadrant case 1
				((x1>x2) && (y1<y2) && x1>0 && y1<0 && x2<=0 && y2<=0) ||	//from 2 to 3 quadrant case 2
				((x1>x2) && (y1<y2) && x1<=0 && y1<=0 && x2<0 && y2>0) ||	//from 3 to 4 quadrant case 4
				((x1>x2) && (y1<y2) && x1>=0 && y1<=0 && x2<=0 && y2>=0) 		//from 4 to 2 quadrant case 2
				)
		{		
			rotation=(Math.Asin(Math.Abs(y2-y1)/tr_length))+pi;
		}
		else if (
				((x1>x2) && (y1>y2) && x1>=0 && y1<=0 && x2<0 && y2<0) ||	//from 2 to 3 quadrant case 4
				((x1>x2) && (y1>y2) && x1<=0 && y1>=0 && x2<0 && y2<0) ||	//from 3 to 4 quadrant case 2
				((x1>x2) && (y1>y2) && x1>=0 && y1>=0 && x2<=0 && y2<=0) ||	//from 1 to 3 quadrant case 2
				
				((y1>y2) && x1==x2) ||	//horizontal case1
				((x1>x2) && y1==y2) 	//vertical case 1
				)
		{
			rotation=-(Math.Asin(Math.Abs(y2-y1)/tr_length))-pi;
		}
		///////////////////
		else
		{
			rotation=double.NaN;
			Console.WriteLine("During transfromation the member position doesn't found!!");
		}
		
		//transformation matrix to local coord
		//finally this matrix isn't used due problem with some cases but is used for inversion
		double[,] matrix= new double[,]
		{
			{Math.Cos(rotation),-Math.Sin(rotation),Math.Cos(rotation)*movex-Math.Sin(rotation)*movey},
			{Math.Sin(rotation),Math.Cos(rotation),Math.Sin(rotation)*movex+Math.Cos(rotation)*movey},
			{0.0,0.0,1.0},
		};
			
		/*//checking
		Console.WriteLine("length: " + length);
		Console.WriteLine("rotation: " + rotation);
		Console.WriteLine(matrix[0,0]);
		Console.WriteLine(matrix[0,1]);
		Console.WriteLine(matrix[0,2]);
		Console.WriteLine(matrix[1,0]);
		Console.WriteLine(matrix[1,1]);
		Console.WriteLine(matrix[1,2]);
		Console.WriteLine(matrix[2,0]);
		Console.WriteLine(matrix[2,1]);
		Console.WriteLine(matrix[2,2]);*/
		
		//transform to local
		
		if (toLocal==true)
		{
		
			LocCoord[0]=matrix[0,0]*input_Y_coord+matrix[0,1]*input_Z_coord+matrix[0,2]*1;
			LocCoord[1]=matrix[1,0]*input_Y_coord+matrix[1,1]*input_Z_coord+matrix[1,2]*1;
			return LocCoord;
		}
		
		else if (toLocal==false)
		{
			//transformation matrix to global coord
			double[,] result= new double[,]
			{
				{0.0,0.0,0.0},
				{0.0,0.0,0.0},
				{0.0,0.0,0.0},
			};
			
			//inversion of matrix
			double determinant = +matrix[0,0]*(matrix[1,1]*matrix[2,2]-matrix[2,1]*matrix[1,2])
								-matrix[0,1]*(matrix[1,0]*matrix[2,2]-matrix[1,2]*matrix[2,0])
								+matrix[0,2]*(matrix[1,0]*matrix[2,1]-matrix[1,1]*matrix[2,0]);
			double invdet = 1/determinant;
			result[0,0] =  (matrix[1,1]*matrix[2,2]-matrix[2,1]*matrix[1,2])*invdet;
			result[0,1] = -(matrix[0,1]*matrix[2,2]-matrix[0,2]*matrix[2,1])*invdet;
			result[0,2] =  (matrix[0,1]*matrix[1,2]-matrix[0,2]*matrix[1,1])*invdet;
			result[1,0] = -(matrix[1,0]*matrix[2,2]-matrix[1,2]*matrix[2,0])*invdet;
			result[1,1] =  (matrix[0,0]*matrix[2,2]-matrix[0,2]*matrix[2,0])*invdet;
			result[1,2] = -(matrix[0,0]*matrix[1,2]-matrix[1,0]*matrix[0,2])*invdet;
			result[2,0] =  (matrix[1,0]*matrix[2,1]-matrix[2,0]*matrix[1,1])*invdet;
			result[2,1] = -(matrix[0,0]*matrix[2,1]-matrix[2,0]*matrix[0,1])*invdet;
			result[2,2] =  (matrix[0,0]*matrix[1,1]-matrix[1,0]*matrix[0,1])*invdet;
			
			/*
			//checking
			Console.WriteLine("Transp matrix");
			Console.WriteLine(result[0,0]);
			Console.WriteLine(result[0,1]);
			Console.WriteLine(result[0,2]);
			Console.WriteLine(result[1,0]);
			Console.WriteLine(result[1,1]);
			Console.WriteLine(result[1,2]);
			Console.WriteLine(result[2,0]);
			Console.WriteLine(result[2,1]);
			Console.WriteLine(result[2,2]);
			*/
			//transform to global
			
			GlobCoord[0]=result[0,0]*input_Y_coord+result[0,1]*input_Z_coord+result[0,2]*1;
			GlobCoord[1]=result[1,0]*input_Y_coord+result[1,1]*input_Z_coord+result[1,2]*1;
							
			return GlobCoord;
		}
		
		else   //if something is wrong the transformation function returns NaN values
		{
		Console.WriteLine("Transformation error");
		double [] temp = new double [2];
		temp[0]=double.NaN;
		temp[1]=double.NaN;
		return temp;
		
		}

	}
}	
public class MyForm:Form
{
	public float tempVarAy;
	public float tempVarAz;
	public float tempVarBy;
	public float tempVarBz;
	
	public float tempVarA1y;
	public float tempVarA1z;
	public float tempVarB1y;
	public float tempVarB1z;
	
	public bool AisConn;
	public bool BisConn;
	
	//public MyForm(double ay_,double az_,double by_,double bz_)
	public MyForm(Element myPassedElement)
	{
		//convert double variables to float
		float tempAy = (float)myPassedElement.GlobA.Y;
		float tempAz = (float)myPassedElement.GlobA.Z;
		float tempBy = (float)myPassedElement.GlobB.Y;
		float tempBz = (float)myPassedElement.GlobB.Z;
		float tempA1y = (float)myPassedElement.A1.Y;
		float tempA1z = (float)myPassedElement.A1.Z;
		float tempB1y = (float)myPassedElement.B1.Y;
		float tempB1z = (float)myPassedElement.B1.Z;
		
		
		tempVarAy = tempAy;
		tempVarAz = tempAz;
		tempVarBy = tempBy;
		tempVarBz = tempBz;
		tempVarA1y = tempA1y;
		tempVarA1z = tempA1z;
		tempVarB1y = tempB1y;
		tempVarB1z = tempB1z;
		
		AisConn=myPassedElement.A.isConnected;
		BisConn=myPassedElement.B.isConnected;
		
		
		//set form properties
		this.Text = "Graphics output";
		this.Width = 1000;
		this.Height = 1000;
		this.Paint += new PaintEventHandler(f1_paint);
	}
	private void f1_paint(object sender,PaintEventArgs e)
	{	
			
		Graphics g = e.Graphics;
		
		//transformation values due to negative coordinates
		float transy=500f;
		float transz=500f;
		
		// drawing x and y axis
		g.DrawLine(new Pen(Color.Black,1),transy+500,transz+0,transy-500,transz+0);
		g.DrawLine(new Pen(Color.Black,1),transy+0,transz+500,transy+0,transz-500);
		
		//the element 
		g.DrawLine(new Pen(Color.Black,5),transy+tempVarAy,transz-tempVarAz,transy+tempVarBy,transz-tempVarBz);
		//the effective parts
		g.DrawLine(new Pen(Color.Red,3),transy+tempVarAy,transz-tempVarAz,transy+tempVarA1y,transz-tempVarA1z);
		g.DrawLine(new Pen(Color.Red,3),transy+tempVarBy,transz-tempVarBz,transy+tempVarB1y,transz-tempVarB1z);
		
		//ends
		float radius=20f;
		if (AisConn==true)
		{
		g.DrawEllipse(new Pen(Color.Black,2),transy+tempVarAy-radius/2,transz-tempVarAz-radius/2,radius,radius);
		}
		if (BisConn==true)
		{
		g.DrawEllipse(new Pen(Color.Black,2),transy+tempVarBy-radius/2,transz-tempVarBz-radius/2,radius,radius);
		}
	}
}