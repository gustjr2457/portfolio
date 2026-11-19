(function(){
  var tip=document.getElementById('tip');
  function put(x,y){
    tip.style.left=Math.max(8,Math.min(x+14,window.innerWidth-tip.offsetWidth-8))+'px';
    tip.style.top=Math.max(8,Math.min(y+16,window.innerHeight-tip.offsetHeight-8))+'px';
  }
  function show(e){
    var el=e.currentTarget;tip.textContent=el.getAttribute('data-tip');tip.style.display='block';
    if(e.clientX!==undefined&&e.type!=='focus'){put(e.clientX,e.clientY);}
    else{var r=el.getBoundingClientRect();put(r.left+r.width/2,r.top);}
  }
  function move(e){put(e.clientX,e.clientY);}
  function hide(){tip.style.display='none';}
  document.querySelectorAll('[data-tip]').forEach(function(el){
    el.addEventListener('mouseenter',show);el.addEventListener('mousemove',move);
    el.addEventListener('mouseleave',hide);el.addEventListener('focus',show);el.addEventListener('blur',hide);
    el.addEventListener('click',show);
  });
})();