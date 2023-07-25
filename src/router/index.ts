import { createRouter, createWebHashHistory } from 'vue-router'


const home = () => import('../views/index.vue')
const list = () => import('../views/list.vue')



const router = createRouter({
    history: createWebHashHistory(),
    routes: [
        { path: '/', name: 'index', component: home },
        {
            path: '/list', name: 'list', component: list
        }
    ]
})

export default router