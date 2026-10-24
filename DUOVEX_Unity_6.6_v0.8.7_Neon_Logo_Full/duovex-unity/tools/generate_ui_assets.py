from PIL import Image, ImageDraw, ImageFilter
from pathlib import Path
import math, random
R=Path(__file__).resolve().parent.parent/'Assets/Resources/Visual082'; R.mkdir(exist_ok=True,parents=True)
BLACK=(8,9,13,255); BLACK2=(15,15,22,255); WHITE=(247,247,250,255); PURPLE=(163,100,253,255); CYAN=(102,226,249,255)

def save(im,name): im.save(R/(name+'.png'))
def diagonal_background(kind):
    w,h=1280,720
    im=Image.new('RGBA',(w,h),BLACK); d=ImageDraw.Draw(im,'RGBA')
    # Angular wedge with wide negative space, not a flattened screenshot.
    if kind=='main':
        d.polygon([(140,0),(750,0),(500,275),(282,0)],fill=WHITE)
        d.polygon([(507,283),(1020,720),(268,720)],fill=WHITE)
        d.polygon([(510,275),(630,160),(760,0),(590,0)],fill=BLACK)
        d.polygon([(490,300),(675,720),(825,720),(530,298)],fill=(231,232,238,255))
        d.polygon([(495,310),(580,416),(715,580),(762,646),(731,669),(543,413)],fill=(18,16,28,255))
        d.line([(500,281),(820,0)],fill=PURPLE,width=3)
        d.line([(470,308),(270,720)],fill=PURPLE,width=3)
        d.line([(510,311),(820,720)],fill=(65,45,94,255),width=2)
    else:
        d.polygon([(155,0),(595,0),(330,272)],fill=WHITE)
        d.polygon([(475,325),(825,720),(152,720)],fill=WHITE)
        d.polygon([(530,320),(690,720),(846,720)],fill=(239,238,245,255))
        d.line([(485,335),(288,720)],fill=PURPLE,width=3)
        d.line([(585,0),(375,235)],fill=PURPLE,width=2)
        d.rectangle([558,72,1256,624],fill=(6,8,15,234),outline=(83,66,109,255),width=2)
    # Keep the foreground free of a distracting grid: fine diagonal rules only.
    d.line([(1120,68),(1050,150)],fill=(148,101,205,60),width=1)
    d.line([(1195,95),(1140,170)],fill=(148,101,205,60),width=1)
    for i in range(110):
        rng=random.Random(1942+i)
        x=rng.randrange(w); y=rng.randrange(h)
        if im.getpixel((x,y))[:3]==BLACK[:3]: d.ellipse((x,y,x+1,y+1),fill=(199,171,250,55))
    save(im,'Bg'+kind.title())

def plate(name, w,h, base, edge, accent, cut=36, slash=True, fill_alpha=None):
    im=Image.new('RGBA',(w,h),(0,0,0,0));d=ImageDraw.Draw(im)
    shape=[(cut,2),(w-4,2),(w-cut, h-3),(3,h-3)]
    d.polygon(shape,fill=base)
    d.line(shape+[shape[0]],fill=edge,width=3,joint='curve')
    if slash:
        d.polygon([(cut+1,4),(cut+15,4),(11,h-4),(1,h-4)],fill=accent)
        d.polygon([(cut+22,5),(cut+26,5),(22,h-5),(18,h-5)],fill=edge)
    save(im,name)

diagonal_background('main');diagonal_background('select')
plate('BtnWhite',700,110,WHITE,(20,19,29,255),PURPLE,58)
plate('BtnDark',700,110,(10,11,18,255),WHITE,PURPLE,58)
plate('BtnPurple',700,110,(160,94,250,255),WHITE,(255,255,255,255),58)
plate('CardIdle',760,92,(24,23,32,255),(78,72,96,255),PURPLE,23)
plate('CardSelected',760,92,WHITE,PURPLE,PURPLE,23)
plate('InfoWhite',780,500,WHITE,(18,16,26,255),PURPLE,48)
plate('InfoDark',780,500,(12,13,21,255),PURPLE,PURPLE,48)
plate('SmallPlate',430,78,(15,16,24,255),WHITE,PURPLE,25)
# bespoke geometric, copyright-free placeholder cover icons
labels=['TUTORIAL','INTERLOCK','OVERDRIVE','LIGHT IT UP','LIGHT IT UP','KETSUDAN','EUPHORIANIC','EUPHORIANIC','DESAPARECER','DESAPARECER','OVERWRITE','OVERWRITE','LOCAL']
palettes=[((70,224,243),(117,85,245)),((118,106,227),(216,122,255)),((255,111,120),(227,167,56)),((156,103,247),(96,209,250)),((166,107,248),(96,218,255)),((246,75,112),(192,32,82)),((70,219,233),(159,111,249)),((70,219,233),(159,111,249)),((240,150,98),(156,93,239)),((240,150,98),(156,93,239)),((103,159,255),(181,93,252)),((103,159,255),(181,93,252)),((150,160,175),(110,111,130))]
for k,(a,b) in enumerate(palettes):
    s=320;im=Image.new('RGBA',(s,s),(6,8,18,255));d=ImageDraw.Draw(im,'RGBA');rng=random.Random(991+k//2)
    for y in range(s):
        t=y/(s-1);c=tuple(round((a[j]*(1-t)+b[j]*t)*.45) for j in range(3));d.line((0,y,s,y),fill=(*c,255))
    for j in range(24):
        x=rng.randrange(-100,400); y=rng.randrange(-100,400)
        d.polygon([(x,y),(x+rng.randrange(40,180),y-50),(x+90,y+180)],fill=(*b,rng.randrange(18,75)))
    d.ellipse((55,50,265,260),outline=(*a,250),width=7)
    d.ellipse((85,80,235,230),outline=(*b,240),width=2)
    d.line((66,210,245,95),fill=(*a,255),width=18)
    d.line((77,105,250,230),fill=(*b,255),width=12)
    d.polygon([(145,108),(204,160),(145,215),(93,160)],fill=(248,250,255,208))
    d.polygon([(145,136),(174,160),(145,187),(116,160)],fill=(25,15,45,255))
    d.text((18,290),f'DV / {k+1:02}',fill=(230,235,245,255))
    save(im,'Cover'+str(k))
print('assets:',len(list(R.iterdir())))
