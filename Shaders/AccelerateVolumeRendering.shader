Shader "Unlit/AccelerateVolumeRendering"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VolumeScale("VolumeScale", Vector) = (1,1,1,1)
        _Dimensions("Dimensions", Vector) = (512,512,512)
        _BlockSize("BlockSize", int) = 4
        _Volume("Volume", 3D) = "white" {}
        _OccupancyMap("OccupancyMap", 3D) = "white" {}
        _DistanceMap("DistanceMap", 3D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Front
        
        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 vray_dir : TEXCOORD0;
                float3 transformed_eye : float3;
                float3 position : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _VolumeScale;
            float3 _Dimensions;
            int _BlockSize;
            sampler3D _Volume;
            sampler3D _OccupancyMap;
            sampler3D _DistanceMap;
            float4x4 _Rotation;
            float4x4 _Translation;
            float4x4 _Scale;

            v2f vert (appdata v)
            {
                v2f o;
                float3 volume_translation = float3(0.5,0.5,0.5) - _VolumeScale.xyz * 0.5;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.transformed_eye = mul(_Scale, mul(_Rotation, mul(_Translation, float4(_WorldSpaceCameraPos,1)))).xyz;
                o.vray_dir = v.vertex.xyz  - o.transformed_eye;
                return o;
            }

            float2 intersect_box(float3 start, float3 dir){
                float3 box_min = float3(-0.5f,-0.5f,-0.5f);
                float3 box_max = float3(0.5f,0.5f,0.5f);
                //float3 box_min = float3(0,0,0);
                //float3 box_max = float3(1,1,1);
                float3 inverse_dir = 1.0f / dir;
                float3 tmin_temp = (box_min - start) * inverse_dir;
                float3 tmax_temp = (box_max - start) * inverse_dir;
                float3 tmin = min(tmin_temp, tmax_temp);
                float3 tmax = max(tmin_temp, tmax_temp);
                float t0 = max(tmin.x, max(tmin.y, tmin.z));
                float t1 = min(tmax.x, min(tmax.y, tmax.z));
                return float2(t0, t1);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                
                float3 ray_dir = normalize(i.vray_dir);
                float2 t_hit = intersect_box(i.transformed_eye, ray_dir);
                
                if(t_hit.x>t_hit.y) discard;

                t_hit.x = max(t_hit.x, 0);
                
                float3 dt_vec = 1 / float3(float3(512,512,512) * abs(ray_dir));
                float dt = min(dt_vec.x, min(dt_vec.y, dt_vec.z));
                float3 deltaM = ray_dir * dt;
                float3 deltauM = _Dimensions / _BlockSize * deltaM;

                float col = 0;
                float alpha = 0;
                float3 start = i.transformed_eye + t_hit.x * ray_dir;
                float dist = (t_hit.y-t_hit.x);
                float3 p = start;

                for(int t=0; t<1000; t++){
                    float3 uv = p+0.5;
                    float isOccupied = tex3Dlod(_OccupancyMap,float4(uv,0));
                    if(isOccupied==1){

                        float val = tex3Dlod(_Volume,float4(uv,0));
                        col = max(col,val);
                        alpha = max(alpha,val);
                        if(alpha>=0.98||distance(p,start)>dist){
                            break;
                        }   
		                    p += deltaM;
                    }
                    else{      
                        float3 roundUv = round(uv*_Dimensions/_BlockSize)*_BlockSize/_Dimensions;
                        float3 rM = roundUv-uv;
                        float D = tex3Dlod(_DistanceMap,float4(uv,0))*128;
                        float3 deltai = ceil((step(0,-deltaM)+sign(deltaM)*D+rM)/deltaM);
                        int step = max(min(min(deltai.x,deltai.y),deltai.z),1);
                        p += step*deltaM;
                    }
                    if(alpha>=0.98||distance(p,start)>dist){
                        break;
                    }
                }
              //  for(int t = 0; t< 500; t ++){ 
                   
              //      float val = tex3D(_Volume,p+0.5);
              //      col = max(col,val);
              //      alpha = max(alpha,val);
                    
              //      if(alpha>=0.98||t_hit.x+t*dt>t_hit.y){
              //          break;
              //      }

		            //p += ray_dir * dt;
              //  }
                return fixed4(col,col,col,alpha);
            }
            ENDCG
        }
    }
}
