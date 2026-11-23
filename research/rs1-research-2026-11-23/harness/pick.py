exec(open('an3.py').read().split("print('slots'")[0])
import random
base=[0x1686,0x162F,0x1627,0x160F,0x1607,0x1606,0x1626,0x128C,0x12EC,0x12ED,0x12EE,0x15E4,0x15E5,0x181C,0x129B,0x12FC,0x12FD,0x12FE,0x1304,0x1305,0x1306]
base2=[a for a in base if a not in (0x181C,0x12EC)]
G=collections.defaultdict(list)
for d in rows:
    if d['opm']=='12': G[d['T']].append(d)
pairs=[]
for t,v in G.items():
    sub=collections.defaultdict(list)
    for x in v: sub[tuple(x['mem'].get(a,-1) for a in base2)].append(x)
    for xs in sub.values():
        hs={}
        for x in xs: hs.setdefault(x['h'],x)
        if len(hs)>1:
            a,b=list(hs.values())[:2]
            pairs.append((t,a['p'],b['p'],a['mem'][0x181C],b['mem'][0x181C],a['mem'][0x12EC],b['mem'][0x12EC],a['last'],b['last']))
random.seed(1); random.shuffle(pairs)
seen=set(); out=[]
for p in pairs:
    if p[0] in seen: continue
    seen.add(p[0]); out.append(p)
for p in out[:14]: print(*p)
open('pairs.txt','w').write('\n'.join(f'{p[1]} {p[2]}' for p in out[:14]))
