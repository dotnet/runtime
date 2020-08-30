// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Build a Directed Graph with 100 nodes
// Test KeepAlive for huge directed graphs

namespace Default {

using System;

public class Graph
{
	private Vertex Vfirst = null;
	private Vertex Vlast = null;
	private Edge Efirst = null;
	private Edge Elast = null;
	private int WeightSum = 0;
	
	public static int Nodes;
	public static bool flag;

	public Graph(int n) { Nodes = n;}

	public void SetWeightSum() {
		Edge temp = Efirst;
		WeightSum = 0;
		while(temp != null) {
			WeightSum += temp.Weight;
			temp = temp.Next;
		}
	}
	
	public int GetWeightSum() {
		return WeightSum;
	}
		
	public void BuildEdge(int v1,int v2) {
		Vertex n1 = null,n2 = null;
		Vertex temp = Vfirst;
		
		while(temp != null) {
            if (v1 == temp.Name)
            {
				//found 1st node..
				n1 = temp;
				break;
			}
			else temp = temp.Next;
		}
		
		//check if edge already exists
		for(int i=0;i<n1.Num_Edges;i++) {
            if (v2 == n1.Adjacent[i].Name)
                return;
		}

		temp = Vfirst;
		while(temp != null) {
            if (v2 == temp.Name)
            {
				//found 2nd node..
				n2 = temp;
				break;
			}
			else temp = temp.Next;
		}
		
		n1.Adjacent[n1.Num_Edges++]=n2;
		
		Edge temp2 = new Edge(n1,n2);
		        if(Efirst==null) {
		                Efirst = temp2;
		                Elast = temp2;
		        }
		        else {
		                temp2.AddEdge(Elast,temp2);
		                Elast = temp2;
                       }
	}
	
	public void BuildGraph() {
		
		// Build Nodes	
		Console.WriteLine("Building Vertices...");
		for(int i=0;i< Nodes; i++) {
			Vertex temp = new Vertex(i);
			if(Vfirst==null) {
			     Vfirst = temp;
			     Vlast = temp;
		        }
		        else {
			     temp.AddVertex(Vlast,temp);
			     Vlast = temp;
                        }
		}
		
		// Build Edges
		Console.WriteLine("Building Edges...");
	
        Int32 seed = Environment.TickCount;
		Random rand = new Random(seed);
		
		for(int i=0;i< Nodes;i++) {
		    
		    int j = rand.Next(0,Nodes);
		    for(int k=0;k<j;k++) {
		       int v2;
		       while((v2 = rand.Next(0,Nodes))==i);     //select a random node, also avoid self-loops
		       BuildEdge(i,v2);                //build edge betn node i and v2
	              
		       
		    }		
		}
	}


	public void CheckIfReachable() {
		int[] temp = new int[Nodes];
		Vertex t1 = Vfirst;
		
		Console.WriteLine("Making all vertices reachable...");
		while(t1 != null) {
			for(int i=0;i<t1.Num_Edges;i++) {
				if(temp[t1.Adjacent[i].Name] == 0)
					temp[t1.Adjacent[i].Name]=1;
			}
			t1 = t1.Next;
		}

		for(int v2=0;v2<Nodes;v2++) {
			if(temp[v2]==0) {  //this vertex is not connected
                Int32 seed = Environment.TickCount;
				Random rand = new Random(seed);
				int v1;
				while((v1 = rand.Next(0,Nodes))==v2);     //select a random node, also avoid self-loops
				BuildEdge(v1,v2);
				temp[v2]=1;
			}
		}
		
	}
	

	public void DeleteVertex() {

		DeleteVertex(Vfirst);
		
	}

	public void DeleteVertex(Vertex v) {
		if(v == Vlast) {
			Vfirst=null;
			Vlast=null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			return;
		}
		Vertex temp = v.Next;
		v=null;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		DeleteVertex(temp);
		temp=null;
		GC.Collect();
		GC.WaitForPendingFinalizers();
		
	}

	public Vertex ReturnVfirst() {
		return(Vfirst);
	}
	
}

public class Vertex
	{
		public int Name;
		//public bool Visited = false;
		
		public Vertex Next;
		public Vertex[] Adjacent;
		public Edge[] Edges;
		public int Num_Edges = 0;
		public static int count=0;

		public Vertex(int val) {
			Name = val;
			Next = null;
			Adjacent = new Vertex[Graph.Nodes];	
		}
		
		~Vertex() {
			//Console.WriteLine("In Finalize of Vertex");
			count++;
			if((count==100) && (Graph.flag==false)) {
                Test.exitCode = 1;
			}
		}

		public void AddVertex(Vertex x, Vertex y) {
			x.Next = y;				
		}
		
		public void DeleteAdjacentEntry(int n) {
			int temp=Num_Edges;
			for(int i=0;i< temp;i++) {
				if(n == Adjacent[i].Name) {
					for(int j=i;j<Num_Edges;j++) 
						Adjacent[j] = Adjacent[j+1];
					Num_Edges--;
					return;
				}
			}
		}
	}


public class Edge 
	{
		public int Weight;
		public Vertex v1,v2;
		public Edge Next;
	
		public Edge(Vertex n1, Vertex n2) {
			v1=n1;
			v2=n2;
			
			int seed = n1.Name+n2.Name;
			Random rand = new Random(seed);
			Weight = rand.Next(0,50);
		}
		
		public void AddEdge(Edge x, Edge y) {
			x.Next = y;				
		}

	}


public class Test
{
    public static int exitCode = 0;
  public static int Main()
  {
	Graph.flag=false;
    exitCode = 100;

	Console.WriteLine("Test should pass with ExitCode 100");
	Console.WriteLine("Building Graph with 100 vertices...");
	Graph MyGraph = new Graph(100);  

	MyGraph.BuildGraph();    
	MyGraph.CheckIfReachable();

	Console.WriteLine("Deleting all vertices...");

	MyGraph.DeleteVertex();

	GC.Collect();
	GC.WaitForPendingFinalizers();

	Vertex temp = MyGraph.ReturnVfirst();	
	GC.KeepAlive(temp);	// will keep alive the graph till here

	Console.WriteLine("Done...");
	Graph.flag=true;	// to check if finalizers ran at shutdown or earlier
    return exitCode;
	
  }
}
}
