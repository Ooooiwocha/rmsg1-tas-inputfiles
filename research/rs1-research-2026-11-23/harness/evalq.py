import sys
def load(fs):
    d={}
    for f in fs:
        for l in open(f):
            if l.startswith('NODE'):
                p=l.split(); kv=dict(x.split('=',1) for x in p[2:]); d[int(p[1])]=kv
    return d
d=load(sys.argv[1:])
T=[(0x1ABB57,'BB57'),(0x1AE3F8,'E3F8'),(0x1BAE33,'AE33'),(0x1BAF88,'AF88'),(0x1BB0B3,'B0B3'),(0x1BB0B2,'B0B2>B0B3'),(0x1BB1BE,'B1BE'),(0x1BB1BD,'B1BD>B1BE'),(0x1BB3BD,'B3BD>2F>B1BE'),(0x1BB3BC,'B3BC>2F>B1BE'),(0x1BB280,'B280'),(0x1BB27F,'B27F>B280'),(0x1BB3C2,'B3C2'),(0x1BB3C1,'B3C1>B3C2'),(0x1BB476,'B476'),(0x1BB475,'B475>B476'),(0x1ABD56,'BD56>2F>BB57')]
def diff(a,t):
    ab,ap=a>>16,(a>>8)&0xff; tb,tp=t>>16,(t>>8)&0xff
    if ab==tb and ap>tp: return ap-tp
    if tb==ab+1: return ap-tp+0x80
    return None
out=[]
for i,v in d.items():
    if v['m']!='12' or v['b']!='12': continue
    a=int(v['addr'],16); f=int(v['f'])-2
    for t,n in T:
        if (a&0xff)!=((t-1)&0xff): continue
        x=diff(a,t)
        if x: out.append((f+x,n,v['addr'],f))
out.sort(); print(out[:8])
