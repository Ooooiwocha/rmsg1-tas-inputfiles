import re,collections,sys
rows=[]
for f in ['s3A.txt','s3B.txt']:
    for l in open(f):
        if 'nojump' in l: continue
        d=dict(re.findall(r'(\w+)=(\S+)',l)); k=d['keys']; 
        by=bytes.fromhex(k)
        mem={}
        for i in range(0x100): mem[0x1200+i]=by[i]
        for i in range(0x100): mem[0x1600+i]=by[0x100+i]
        mem[0x181C]=by[0x200]; mem[0x15E4]=by[0x201]; mem[0x15E5]=by[0x202]
        sp=(by[0x203]<<8)|by[0x204]; d['sp']=sp
        for j,q in enumerate(range(sp+1,0x1E00)): mem[q]=by[0x205+j]
        d['mem']=mem; rows.append(d)
def run(name,addrs,slot=None,stackall=False,allmem=False):
    G=collections.defaultdict(list)
    for d in rows:
        if slot and d['opm']!=slot: continue
        G[d['T']].append(d)
    unexpl=0; nontriv=0; vac=0; badruns=0; bad=[]
    for t,v in G.items():
        if len(set(x['h'] for x in v))<2: continue
        sub=collections.defaultdict(list)
        for x in v:
            if allmem: key=tuple(sorted(x['mem'].items()))
            else:
                key=tuple(x['mem'].get(a,-1) for a in addrs)
                if stackall: key=key+(x['sp'],tuple(sorted((a,b) for a,b in x['mem'].items() if a>=0x1C00)))
            sub[key].append(x)
        b=[xs for xs in sub.values() if len(set(x['h'] for x in xs))>1]
        if b: unexpl+=1; bad.append((t,b))
        elif any(len(xs)>1 for xs in sub.values()): nontriv+=1
        else: vac+=1
    print(f'{name}: divergent targets explained(nontrivial)={nontriv} vacuous={vac} unexplained={unexpl}')
    return bad
print('slots',collections.Counter(d['opm'] for d in rows))
base=[0x1686,0x162F,0x1627,0x160F,0x1607,0x1606,0x1626,0x128C,0x12EC,0x12ED,0x12EE,0x15E4,0x15E5,0x181C,0x129B,0x12FC,0x12FD,0x12FE,0x1304,0x1305,0x1306]
for slot in ['12','13','11']:
    print('== slot',slot)
    b0=run('verified only',base,slot)
    run('+cands(121F,1255,1290,16CA,1DA6,1DAC)',base+[0x121F,0x1255,0x1290,0x16CA,0x1DA6,0x1DAC],slot)
    run('+whole stack',base,slot,stackall=True)
    b3=run('all captured',[],slot,allmem=True)
    if slot=='12':
        import pickle; pickle.dump((b0,b3),open('bad.pkl','wb'))
print('---- detail')
b0,b3=pickle.load(open('bad.pkl','rb'))
for t,bl in b0:
    for xs in bl:
        print(t,[(x['p'],x['last']) for x in xs][:8])
base2=[a for a in base if a not in (0x181C,0x12EC)]
print('== slot 12 without 181C, 12EC')
b=run('verified minus 181C/12EC',base2,'12')
for t,bl in b: print(' ',t,len(bl),[len(xs) for xs in bl][:6], collections.Counter(x['last'] for xs in bl for x in xs).most_common(4))
